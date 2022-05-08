using System.IO;
using Newtonsoft.Json;

namespace TP_Link.Kasa.Bulb
{
    internal static class AppData
    {
        /*Use an arbitrary key for encryption.
        *while the encrypted data can easily be reversed if this key is taken from the source,
        *it will help prevent any random user on the system from just going and reading the contents of the file in plain text
        */
        private static readonly string aesKey = "687ea0f6ff13493c97e2417ca04f24c7";
        private static readonly object lockObject = new object();
        private static readonly string appDataPath = "./appdata.dat";

        internal static CAppData data;

        internal static CAppData Load()
        {
            lock (lockObject)
            {
                if (!File.Exists(appDataPath)) data = new CAppData();
                else data = JsonConvert.DeserializeObject<CAppData>(AES.DecryptString(aesKey, File.ReadAllText(appDataPath)));
                return data;
            }
        }

        internal static void Save()
        {
            lock (lockObject)
            {
                File.WriteAllText(appDataPath, AES.EncryptString(aesKey, JsonConvert.SerializeObject(data)));
            }
        }

        internal class CAppData
        {
            public string email;
            public string password;
        }
    }
}
