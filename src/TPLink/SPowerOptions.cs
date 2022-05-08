using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SPowerOptions
    {
        public int hue;
        public int saturation;
        public int brightness;
        [JsonProperty("color_temp")]
        public int colorTemp;
    }
}
