using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SGetSysinfo
    {
        [JsonProperty("sw_ver")]
        public string swVer;
        [JsonProperty("hw_ver")]
        public string hwVer;
        public string model;
        public string description;
        public string alias;
        [JsonProperty("mic_type")]
        public string micType;
        [JsonProperty("dev_state")]
        public string devState;
        [JsonProperty("mic_mac")]
        public string micMac;
        public string deviceId;
        public string oemId;
        public string hwId;
        [JsonProperty("is_factory")]
        public bool isFactory;
        [JsonProperty("disco_ver")]
        public string discoVer;
        [JsonProperty("relay_sctrl_protocolstate")]
        public SCtrlProtocols ctrlProtocols;
        [JsonProperty("light_state")]
        public SLightState lightState;
        [JsonProperty("is_dimmable")]
        public int isDimmable;
        [JsonProperty("is_color")]
        public int isColor;
        [JsonProperty("is_variable_color_temp")]
        public int isVariableColorTemp;
        public IReadOnlyList<SPreferredState> preferred_state;
        public int rssi;
        [JsonProperty("active_mode")]
        public string activeMode;
        public int heapsize;
        [JsonProperty("err_code")]
        public int errCode;

        [JsonProperty("relay_state")]
        public object relaystate;
    }
}
