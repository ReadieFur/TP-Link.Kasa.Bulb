using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace TP_Link.Kasa.Bulb
{
    public static class Scripts
    {
        private static readonly string scriptsDirectory = "./scripts";
        private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        private static List<string> _scripts = new List<string>();
        public static IReadOnlyList<string> scripts
        {
            get { return _scripts; }
            private set {}
        }

        static Scripts()
        {
            if (!Directory.Exists(scriptsDirectory)) Directory.CreateDirectory(scriptsDirectory);
            else LoadScriptsInfo();
        }

        public static void AddScript(string name, List<TPLink.SLightState> data)
        {
            if (_scripts.Contains(name)) throw new Exception("Names must be unique.");
            _scripts.Add(name);
            SaveScript(name, data);
        }

        public static List<TPLink.SLightState> LoadScript(string name)
        {
            if (!_scripts.Contains(name)) throw new KeyNotFoundException();
            List<TPLink.SLightState> frames = JsonConvert.DeserializeObject<List<TPLink.SLightState>>(
                File.ReadAllText($"{scriptsDirectory}/{name}.json"), jsonSerializerSettings);
            return frames != null ? frames : new List<TPLink.SLightState>();
        }

        public static void UpdateScript(string name, List<TPLink.SLightState> data, string newName = null)
        {
            if (!_scripts.Contains(name)) throw new KeyNotFoundException(name);
            if (newName != null)
            {
                _scripts.Remove(name);
                _scripts.Add(newName);
            }
            SaveScript(newName??name, data);
        }

        public static void RemoveScript(string name)
        {
            if (!_scripts.Contains(name)) throw new KeyNotFoundException();
            _scripts.Remove(name);
            File.Delete($"{scriptsDirectory}/{name}.json");
        }

        public static void LoadScriptsInfo()
        {
            List<string> __scripts = new List<string>();
            foreach (string file in Directory.GetFiles(scriptsDirectory, "*.json")) __scripts.Add(Path.GetFileNameWithoutExtension(file));
            _scripts = __scripts;
        }

        private static void SaveScript(string name, List<TPLink.SLightState> data)
        {
            File.WriteAllText($"{scriptsDirectory}/{name}.json", JsonConvert.SerializeObject(data, jsonSerializerSettings));
        }
    }
}
