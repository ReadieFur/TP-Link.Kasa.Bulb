using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SSchedule
    {
        [JsonProperty("get_daystat")]
        public SGetDayStat getDatStat;
        [JsonProperty("get_rules")]
        public object getRules;
    }
}
