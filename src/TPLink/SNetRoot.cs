using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SNetRoot
    {
        [JsonProperty("get_scaninfo")]
        public SGetScanInfo getScanInfo;
        [JsonProperty("set_stainfo")]
        public SSetSTAInfo staInfo;
    }
}
