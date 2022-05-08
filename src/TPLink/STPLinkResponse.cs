using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct STPLinkResponseRoot<T>
    {
        [JsonProperty("error_code")]
        public int errorCode;
        public string msg;
        public T result;
    }
}
