using RiotGames;
using RtmpSharp.IO;
using System;
using System.Linq;

namespace WintermintClient.Data
{
    internal static class RtmpSharpData
    {
        public static Type[] SerializableTypes;

        public static SerializationContext SerializationContext;

        static RtmpSharpData()
        {
            RtmpSharpData.SerializableTypes = RiotDto.GetSerializableTypes().ToArray<Type>();
            RtmpSharpData.SerializationContext = new SerializationContext(RtmpSharpData.SerializableTypes);
        }
    }
}