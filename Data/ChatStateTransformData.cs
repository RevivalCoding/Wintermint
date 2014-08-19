using Chat;
using Complete;
using Complete.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace WintermintClient.Data
{
    internal static class ChatStateTransformData
    {
        public static void Initialize()
        {
            ChatStatic.XmlStateTransform = new ChatStatic.XmlStateTransformFunction(ChatStateTransformData.XmlStateTransform);
            ChatStatic.JsonStateTransform = new ChatStatic.JsonStateTransformFunction(ChatStateTransformData.JsonStateTransform);
            ChatStatic.StringStateTransform = new ChatStatic.StringStateTransformFunction(ChatStateTransformData.StringStateTransform);
        }

        private static object JsonStateTransform(JObject obj)
        {
            Func<string, string> func = (string name) =>
            {
                JToken jTokens;
                if (!obj.TryGetValue(name, out jTokens))
                {
                    return null;
                }
                return (string)jTokens;
            };
            ChatStateTransformData.JsStatus jsStatu = new ChatStateTransformData.JsStatus()
            {
                Message = func("message"),
                Status = func("status")
            };
            return jsStatu;
        }

        private static object StringStateTransform(string str)
        {
            return new ChatStateTransformData.JsStatus()
            {
                Message = str,
                Status = "out-of-game"
            };
        }

        private static object XmlStateTransform(XDocument document)
        {
            long num;
            XElement xElement1 = document.Element("body");
            if (xElement1 == null)
            {
                return null;
            }
            Func<string, string> func = (string name) =>
            {
                XElement xElement = xElement1.Element(name);
                if (xElement == null)
                {
                    return null;
                }
                return xElement.Value;
            };
            long.TryParse(func("timeStamp"), out num);
            ChatStateTransformData.JsStatus jsStatu = new ChatStateTransformData.JsStatus()
            {
                Message = func("statusMsg"),
                Status = func("gameStatus").Dasherize()
            };
            ChatStateTransformData.JsGameStatus jsGameStatu = new ChatStateTransformData.JsGameStatus()
            {
                ChampionId = ChampionNameData.GetChampionId(func("skinname")),
                Queue = func("gameQueueType"),
                Started = UnixDateTime.Epoch.AddMilliseconds((double)num)
            };
            jsStatu.Game = jsGameStatu;
            return jsStatu;
        }

        [Serializable]
        private class JsGameStatus
        {
            public int ChampionId;

            public string Queue;

            public DateTime Started;

            public JsGameStatus()
            {
            }
        }

        [Serializable]
        private class JsStatus
        {
            public string Message;

            public string Status;

            public ChatStateTransformData.JsGameStatus Game;

            public JsStatus()
            {
            }
        }
    }
}