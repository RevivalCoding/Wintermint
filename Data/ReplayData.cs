using FileDatabase;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.Data.Extensions;
using WintermintData.Matches;

namespace WintermintClient.Data
{
    public class ReplayData
    {
        private static Dictionary<string, JObject> spectator;

        public ReplayData()
        {
        }

        public async Task<Match> GetSpectatorMatchStats(string realmId, long matchId)
        {
            await Task.Delay(1000);
            return new Match();
        }

        public string GetSpectatorUri(string realmId, string type, params object[] args)
        {
            JObject item = ReplayData.spectator[realmId];
            return string.Format((string)item[type], args);
        }

        public static async Task Initialize(IFileDb fileDb)
        {
            string stringAsync = await fileDb.GetStringAsync("riot/endpoints/spectator.json");
            ReplayData.spectator = stringAsync.Deserialize<Dictionary<string, JObject>>().Desensitize<JObject>();
        }
    }
}