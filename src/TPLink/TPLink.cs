using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public class TPLink
    {
        private string token = null;

        public async Task LogIn(string email, string password, CancellationToken cancellationToken = default)
        {
            token = null;
            SLogInResponse response = await PostRequest<SLogInResponse>(new Dictionary<string, object>
            {
                { "method", "login" },
                {
                    "params", new Dictionary<string, string>
                    {
                        { "appType", "Kasa_Android" },
                        { "cloudUserName", email },
                        { "cloudPassword", password },
                        { "terminalUUID", Guid.NewGuid().ToString() }
                    }
                }
            }, cancellationToken);
            token = response.token;
        }

        public async Task<IEnumerable<SDevice>> GetDevices(CancellationToken cancellationToken = default)
        {
            return (await PostRequest<SDeviceList>(new Dictionary<string, object> { { "method", "getDeviceList" } },
                cancellationToken)).deviceList.Where(d => Data.supportedDevices.Contains(d.deviceModel.Substring(0, 5)));
        }

        public async Task<SDeviceInfo> GetDeviceInfo(string deviceID, CancellationToken cancellationToken = default)
        {
            return await Passthrough<SDeviceInfo>(deviceID, new Dictionary<string, object>
            {{
                "system", new Dictionary<string, object>
                {
                    { "get_sysinfo", new object() }
                }
            }});
        }

        public async Task<SLightState> SetLightState(
            SDevice device,
            bool powerOn,
            int transitionTime = 0,
            SPowerOptions? powerOptions = null,
            SDeviceInfo? deviceInfo = null
        )
        {
            SDeviceInfo _deviceInfo = deviceInfo != null ? (SDeviceInfo)deviceInfo : await GetDeviceInfo(device.deviceId);

            if (_deviceInfo.system.getSYSInfo.relaystate != null)
            {
                //New format.
                var body = new Dictionary<string, object>
                {{
                    "system", new Dictionary<string, object>
                    {
                        { "set_relay_state", new Dictionary<string, int>{{ "state", powerOn ? 1 : 0 }} }
                    }
                }};
                return await Passthrough<SLightState>(device.deviceId, body);
            }
            else
            {
                //Old format.
                SLightState lightState;
                if (powerOptions != null)
                {
                    SPowerOptions _powerOptions = (SPowerOptions)powerOptions;
                    lightState = new SLightState
                    {
                        ignoreDefault = 1,
                        onOff = powerOn ? 1 : 0,
                        transitionPeriod = transitionTime,
                        hue = _powerOptions.hue,
                        saturation = _powerOptions.saturation,
                        brightness = _powerOptions.brightness,
                        colorTemp = _powerOptions.colorTemp
                    };
                }
                else
                {
                    lightState = new SLightState
                    {
                        ignoreDefault = 1,
                        onOff = powerOn ? 1 : 0,
                        transitionPeriod = transitionTime
                    };
                }
                return (await Passthrough<SOldFormat>(device.deviceId, new SOldFormat
                {
                    lightingService = new SLightingService
                    {
                        transitionLightState = lightState
                    }
                })).lightingService.transitionLightState;
            }
        }

        private async Task<T> Passthrough<T>(string deviceID, object data, CancellationToken cancellationToken = default)
        {
            SPassthroughResponse response = await PostRequest<SPassthroughResponse>(new Dictionary<string, object>
            {
                { "method", "passthrough" },
                {
                    "params", new Dictionary<string, string>
                    {
                        { "deviceId", deviceID },
                        { "requestData", JsonConvert.SerializeObject(data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) }
                    }
                }
            }, cancellationToken);
            return JsonConvert.DeserializeObject<T>(response.responseData);
        }

        private async Task<ResponseType> PostRequest<ResponseType>(object body, CancellationToken cancellationToken = default)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.PostAsync(
                "https://wap.tplinkcloud.com" + (token != null ? $"/?token={token}" : ""),
                new StringContent(
                    JsonConvert.SerializeObject(body, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                    Encoding.UTF8,
                    "application/json"
                ),
                cancellationToken
            );
            httpClient.Dispose();

            if (!response.IsSuccessStatusCode) throw new Exception(response.StatusCode.ToString());
            STPLinkResponseRoot<ResponseType> responseObject =
                JsonConvert.DeserializeObject<STPLinkResponseRoot<ResponseType>>(await response.Content.ReadAsStringAsync());
            if (responseObject.errorCode != 0) throw new Exception(responseObject.msg);
            return responseObject.result;
        }
    }
}
