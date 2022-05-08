using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SOldFormat
    {
        [JsonProperty("smartlife.iot.common.softaponboarding")]
        public SNetRoot accessPoint;
        [JsonProperty("smartlife.iot.smartbulb.lightingservice")]
        public SLightingService lightingService;
        [JsonProperty("smartlife.iot.common.system")]
        public SSystemOld system;
        [JsonProperty("smartlife.iot.common.schedule")]
        public SSchedule schedule;
        [JsonProperty("smartlife.iot.common.cloud")]
        public SCloud cloud;
    }
}
