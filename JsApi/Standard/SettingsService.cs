using MicroApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WintermintClient.Data;
using WintermintClient.JsApi;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("settings.local")]
    public class SettingsService : JsApiService
    {
        private static string StorageLocation;

        static SettingsService()
        {
            SettingsService.StorageLocation = Path.Combine(LaunchData.DataDirectory, "settings");
        }

        public SettingsService()
        {
        }

        [MicroApiMethod("read")]
        public async Task<object> ReadAsync()
        {
            object obj;
            using (StreamReader streamReader = new StreamReader(SettingsService.StorageLocation, Encoding.UTF8))
            {
                obj = JObject.Parse(await streamReader.ReadToEndAsync());
            }
            return obj;
        }

        [MicroApiMethod("store")]
        public async Task StoreAsync(JObject obj)
        {
            using (StreamWriter streamWriter = new StreamWriter(SettingsService.StorageLocation, false, Encoding.UTF8))
            {
                string str = obj.ToString(Formatting.None, new JsonConverter[0]);
                await streamWriter.WriteAsync(str);
            }
        }
    }
}