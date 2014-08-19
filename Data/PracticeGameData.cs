using FileDatabase;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WintermintClient.Data
{
    internal static class PracticeGameData
    {
        private static Regex[] DefaultGameTests;

        public static async Task Initialize(IFileDb fileDb)
        {
            string stringAsync = await fileDb.GetStringAsync("data/game/practice-names.json");
            IJEnumerable<JToken> jTokens = JObject.Parse(stringAsync).Values();
            IEnumerable<string> strs = (
                from x in jTokens
                select (string)x).Distinct<string>();
            PracticeGameData.DefaultGameTests = (
                from x in strs
                select new Regex(x, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToArray<Regex>();
        }

        public static bool IsDefaultGame(string gameName)
        {
            return PracticeGameData.DefaultGameTests.Any<Regex>((Regex x) => x.IsMatch(gameName));
        }
    }
}