using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SLightState
    {
        [JsonProperty("ignore_default")]
        public int ignoreDefault;
        [JsonProperty("on_off")]
        public int onOff;
        [JsonProperty("transition_period")]
        public int transitionPeriod;
        public string mode;
        public int hue;
        public int saturation;
        [JsonProperty("color_temp")]
        public int colorTemp;
        public int brightness;
    }
}
