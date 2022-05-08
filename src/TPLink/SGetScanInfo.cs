using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SGetScanInfo
    {
        public int refresh;
        [JsonProperty("ap_list")]
        public object apList;
    }
}
