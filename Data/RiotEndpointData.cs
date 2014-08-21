using FileDatabase;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WintermintClient.Data
{
    internal static class RiotEndpointData
    {
        private static JObject spectatorEndpoints;

        private static HttpClient http;

        public static async Task Initialize(IFileDb fileDb)
        {
            string stringAsync = await fileDb.GetStringAsync("data/game/runes.json");
            RiotEndpointData.spectatorEndpoints = JObject.Parse(stringAsync);
        }

        private static class Spectate
        {
            private static Task<string> GetAsync(string realmId, string type, params object[] args)
            {
                string uri = RiotEndpointData.Spectate.GetUri(realmId, type, args);
                return RiotEndpointData.http.GetStringAsync(uri);
            }

            public static Task<string> GetMeta(string realmId, long gameId)
            {
                object[] objArray = new object[] { gameId };
                return RiotEndpointData.Spectate.GetAsync(realmId, "meta", objArray);
            }

            private static string GetUri(string realmId, string type, params object[] args)
            {
                string item = (string)RiotEndpointData.spectatorEndpoints[realmId][type];
                return string.Format(item, args);
            }

            public static Task<string> GetVersion(string realmId)
            {
                return RiotEndpointData.Spectate.GetAsync(realmId, "version", new object[0]);
            }
        }
    }
}