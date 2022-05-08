using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SPreferredState
    {
        public int index;
        public int hue;
        public int saturation;
        [JsonProperty("color_temp")]
        public int colorTemp;
        public int brightness;
    }
}
