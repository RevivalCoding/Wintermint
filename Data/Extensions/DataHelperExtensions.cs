using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WintermintClient.Data.Extensions
{
    internal static class DataHelperExtensions
    {
        public static Dictionary<string, T> Desensitize<T>(this Dictionary<string, T> dictionary)
        {
            return new Dictionary<string, T>(dictionary, StringComparer.OrdinalIgnoreCase);
        }

        public static T Deserialize<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}