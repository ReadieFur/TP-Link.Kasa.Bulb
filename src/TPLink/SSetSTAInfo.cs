using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SSetSTAInfo
    {
        public string ssid;
        public string password;
        [JsonProperty("key_type")]
        public int keyType;
        [JsonProperty("cypher_type")]
        public int cypherType;
        [JsonProperty("err_code")]
        public int errCode;
    }
}
