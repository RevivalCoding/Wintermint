using FileDatabase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.Data.Extensions;

namespace WintermintClient.Data
{
    internal static class RuneData
    {
        public static Dictionary<int, Rune> Runes
        {
            get;
            private set;
        }

        public static async Task Initialize(IFileDb fileDb)
        {
            string stringAsync = await fileDb.GetStringAsync("data/game/runes.json");
            RuneData.Runes = stringAsync.Deserialize<Dictionary<int, Rune>>();
        }
    }
}