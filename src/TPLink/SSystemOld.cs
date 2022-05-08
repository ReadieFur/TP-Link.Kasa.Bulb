using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb.TPLink
{
    public struct SSystemOld
    {
        [JsonProperty("set_dev_alias")]
        public SSetDevAlias setDevAlias;
        public object reboot;
    }
}
