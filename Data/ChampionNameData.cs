using FileDatabase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.Data.Extensions;

namespace WintermintClient.Data
{
    internal static class ChampionNameData
    {
        public static Dictionary<string, int> NameToId;

        public static Dictionary<int, string> LegacyIdToClientName;

        public static int GetChampionId(string key)
        {
            int num;
            if (key == null)
            {
                return 0;
            }
            if (!ChampionNameData.NameToId.TryGetValue(key, out num))
            {
                return 0;
            }
            return num;
        }

        public static string GetLegacyChampionClientNameOrSoraka(int championId)
        {
            string str;
            if (!ChampionNameData.LegacyIdToClientName.TryGetValue(championId, out str))
            {
                return "Soraka";
            }
            return str;
        }

        public static async Task Initialize(IFileDb fileDb)
        {
            string stringAsync = await fileDb.GetStringAsync("data/game/champions/mappings/name-to-id.json");
            ChampionNameData.NameToId = stringAsync.Deserialize<Dictionary<string, int>>().Desensitize<int>();
            stringAsync = await fileDb.GetStringAsync("data/game/champions/mappings/legacy/id-to-client-name.json");
            ChampionNameData.LegacyIdToClientName = stringAsync.Deserialize<Dictionary<int, string>>();
        }
    }
}