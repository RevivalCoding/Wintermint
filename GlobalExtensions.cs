using System;
using System.Runtime.CompilerServices;

namespace WintermintClient
{
    public static class GlobalExtensions
    {
        public static string ToProperCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            return string.Concat(char.ToUpperInvariant(str[0]), str.Substring(1).ToLower());
        }
    }
}