using Chat;
using FileDatabase;
using RiotGames.Platform.Summoner;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.Riot;

namespace WintermintClient.JsApi
{
    public abstract class JsApiService
    {
        private static Regex IntegerRegex;

        internal static JsApiService.JsResponse NullResponse;

        internal static LittleClient Client;

        internal static CacheHelper Cache;

        internal static JsApiService.JsPush Push;

        internal static JsApiService.JsPushJson PushJson;

        internal static RiotAccountBag AccountBag
        {
            get
            {
                return Instances.AccountBag;
            }
        }

        internal static IFileDb MediaFiles
        {
            get
            {
                return Instances.MediaFiles;
            }
        }

        internal static RiotAccount RiotAccount
        {
            get
            {
                return Instances.AccountBag.Active;
            }
        }

        internal static IFileDb SupportFiles
        {
            get
            {
                return Instances.SupportFiles;
            }
        }

        static JsApiService()
        {
            JsApiService.IntegerRegex = new Regex("\\d+", RegexOptions.Compiled);
            JsApiService.NullResponse = (object _) =>
            {
            };
            JsApiService.Client = Instances.Client;
            JsApiService.Cache = new CacheHelper();
        }

        protected JsApiService()
        {
        }

        protected static Action<Task<T>> CreatePublisher<T>(ConcurrentDictionary<string, object> dictionary, JsApiService.JsResponse updateFunction, string dictionaryKey)
        {
            return (Task<T> task) =>
            {
                if (task.Status != TaskStatus.RanToCompletion)
                {
                    return;
                }
                dictionary[dictionaryKey] = task.Result;
                updateFunction(dictionary);
            };
        }

        protected static double GetGameStat(Dictionary<string, double> dictionary, string key)
        {
            double num;
            if (!dictionary.TryGetValue(key, out num))
            {
                return 0;
            }
            return num;
        }

        protected static async Task<PublicSummoner> GetSummoner(string realmId, string summonerName)
        {
            RiotAccount riotAccount = JsApiService.AccountBag.Get(realmId);
            PublicSummoner publicSummoner = await riotAccount.InvokeCachedAsync<PublicSummoner>("summonerService", "getSummonerByName", summonerName);
            if (publicSummoner == null)
            {
                riotAccount.RemoveCached<PublicSummoner>("summonerService", "getSummonerByName", summonerName);
            }
            return publicSummoner;
        }

        protected static long GetSummonerIdFromJid(string jid)
        {
            string user = (new JabberId(jid)).User;
            Match match = JsApiService.IntegerRegex.Match(user);
            return long.Parse(match.Value);
        }

        protected static string GetSummonerJidFromId(long summonerId)
        {
            return string.Format("sum{0}@pvp.net", summonerId);
        }

        protected static Task<string> GetSummonerNameByJid(string realm, string jid)
        {
            return JsApiService.GetSummonerNameBySummonerId(realm, JsApiService.GetSummonerIdFromJid(jid));
        }

        protected static async Task<string> GetSummonerNameBySummonerId(string realmId, long summonerId)
        {
            RiotAccount riotAccount = JsApiService.AccountBag.Get(realmId);
            long[] numArray = new long[] { summonerId };
            string[] strArrays = await riotAccount.InvokeCachedAsync<string[]>("summonerService", "getSummonerNames", numArray);
            return strArrays.First<string>();
        }

        protected static bool IsGameStateExitable(string gameState)
        {
            string str = gameState;
            string str1 = str;
            if (str != null && (str1 == "IN_PROGRESS" || str1 == "START_REQUESTED"))
            {
                return false;
            }
            return true;
        }

        protected static void PushIfActive(RiotAccount account, string key, object obj)
        {
            if (JsApiService.AccountBag.Active == account)
            {
                JsApiService.Push(key, obj);
            }
        }

        public delegate void JsPush(string key, object obj);

        public delegate void JsPushJson(string key, string json);

        public delegate void JsResponse(object obj);
    }
}