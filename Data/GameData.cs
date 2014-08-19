using FileDatabase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.Data.Extensions;

namespace WintermintClient.Data
{
    internal static class GameData
    {
        public static Dictionary<string, int[]> SummonerSpells;

        public static Dictionary<int, string> MapIdToName;

        public static int[] GetAvailableSummonerSpells(string gameMode)
        {
            int[] numArray;
            if (GameData.SummonerSpells.TryGetValue(gameMode.ToLowerInvariant(), out numArray))
            {
                return numArray;
            }
            return new int[0];
        }

        public static string GetMapClassification(int mapId)
        {
            string str;
            if (!GameData.MapIdToName.TryGetValue(mapId, out str))
            {
                return "unknown";
            }
            return str;
        }

        public static async Task Initialize(IFileDb fileDb)
        {
            string stringAsync = await fileDb.GetStringAsync("data/game/spells/map-assignments.json");
            GameData.SummonerSpells = stringAsync.Deserialize<Dictionary<string, int[]>>().Desensitize<int[]>();
            stringAsync = await fileDb.GetStringAsync("data/game/maps.json");
            GameData.MapIdToName = stringAsync.Deserialize<Dictionary<int, string>>();
        }
    }
}