using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SLightingService
    {
        [JsonProperty("transition_light_state")]
        public SLightState transitionLightState;
        [JsonProperty("get_light_details")]
        public object getLightDetails;
    }
}
