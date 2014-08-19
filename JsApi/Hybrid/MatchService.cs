using Browser;
using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using RiotGames.Platform.Messaging.Persistence;
using RiotGames.Platform.Statistics;
using RiotSpectate.Spectate;
using RtmpSharp.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.Data;
using WintermintClient.JsApi;
using WintermintClient.Riot;
using WintermintData.Helpers.Matches;
using WintermintData.Matches;
using WintermintData.Riot.Account;

namespace WintermintClient.JsApi.Hybrid
{
    [MicroApiService("match", Preload = true)]
    public class MatchService : JsApiService
    {
        private readonly static EndOfGameMatchTransformer MatchTransformer;

        private readonly Dictionary<string, Match> lastMatches;

        static MatchService()
        {
            MatchService.MatchTransformer = new EndOfGameMatchTransformer(ChampionNameData.NameToId);
        }

        public MatchService()
        {
            this.lastMatches = new Dictionary<string, Match>();
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>((object sender, RiotAccount account) => account.MessageReceived += new EventHandler<MessageReceivedEventArgs>(this.OnMessageReceived));
            JsApiService.AccountBag.AccountRemoved += new EventHandler<RiotAccount>((object sender, RiotAccount account) => account.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(this.OnMessageReceived));
        }

        private static void CacheMatch(Match match)
        {
            CacheHelper cache = JsApiService.Cache;
            string matchCacheKey = MatchService.GetMatchCacheKey(match.RealmId, match.MatchId);
            CacheItemPolicy cacheItemPolicy = new CacheItemPolicy()
            {
                SlidingExpiration = TimeSpan.FromHours(1)
            };
            cache.SetCustom(matchCacheKey, match, cacheItemPolicy);
        }

        private static string GetMatchCacheKey(string realmId, long matchId)
        {
            return string.Format("game#{0}:{1}", realmId, matchId);
        }

        [MicroApiMethod("details")]
        public async Task<object> GetMatchDetails(dynamic args, JsApiService.JsResponse progress, JsApiService.JsResponse result)
        {
            object obj;
            string str = (string)args.realmId;
            long num = (long)args.matchId;
            JObject jObjects = (JObject)args.fill;
            Match matchDetailsInternal = await this.GetMatchDetailsInternal(str, num, (string source) => progress(source));
            Match match = matchDetailsInternal;
            if (match != null)
            {
                if (jObjects != null)
                {
                    JToken item = jObjects["mapId"];
                    if (match.MapId <= 0 && item != null)
                    {
                        match.MapId = (int)item;
                    }
                    string item1 = (string)jObjects["queue"];
                    if (string.IsNullOrEmpty(match.Queue) && !string.IsNullOrEmpty(item1))
                    {
                        match.Queue = item1;
                    }
                }
                JObject jObjects1 = JObject.FromObject(match, SerializerSettings.JsonSerializer);
                dynamic obj1 = jObjects1;
                MatchService.GameRewards additionalData = match.AdditionalData as MatchService.GameRewards;
                if (additionalData != null)
                {
                    obj1.rewards = JObject.FromObject(additionalData, SerializerSettings.JsonSerializer);
                    jObjects1.Remove("additionalData");
                }
                double totalMinutes = match.Length.TotalMinutes;
                int length = (int)match.Teams.Length;
                for (int i = 0; i < length; i++)
                {
                    dynamic obj2 = obj1.teams[i];
                    obj2.index = i;
                    foreach (dynamic obj3 in (IEnumerable)obj2.members)
                    {
                        dynamic obj4 = obj3;
                        obj4.goldPerMinute = (double)obj3.gold / totalMinutes;
                    }
                }
                obj = obj1;
            }
            else
            {
                obj = null;
            }
            return obj;
        }

        public async Task<Match> GetMatchDetailsInternal(string realmId, long matchId, Action<string> source)
        {
            Match match;
            Match match1;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(realmId);
            source("local");
            if (!this.lastMatches.TryGetValue(realmId, out match1) || match1.MatchId != matchId)
            {
                source("cache");
                string matchCacheKey = MatchService.GetMatchCacheKey(realmId, matchId);
                if (JsApiService.Cache.Get(matchCacheKey) == null)
                {
                    source("spectator");
                    Match spectatorMatchAsync = await this.GetSpectatorMatchAsync(riotAccount, matchId);
                    if (spectatorMatchAsync == null)
                    {
                        source("replay");
                        Match replayMatchAsync = await this.GetReplayMatchAsync(riotAccount, matchId);
                        if (replayMatchAsync == null)
                        {
                            source("wintermint");
                            Match wintermintMatchAsync = await this.GetWintermintMatchAsync(riotAccount, matchId);
                            if (wintermintMatchAsync == null)
                            {
                                match = null;
                            }
                            else
                            {
                                JsApiService.Cache.Set(matchCacheKey, wintermintMatchAsync);
                                match = wintermintMatchAsync;
                            }
                        }
                        else
                        {
                            JsApiService.Cache.Set(matchCacheKey, replayMatchAsync);
                            match = replayMatchAsync;
                        }
                    }
                    else
                    {
                        JsApiService.Cache.Set(matchCacheKey, spectatorMatchAsync);
                        match = spectatorMatchAsync;
                    }
                }
                else
                {
                    match = (Match)JsApiService.Cache.Get(matchCacheKey);
                }
            }
            else
            {
                match = match1;
            }
            return match;
        }

        private Task<Match> GetReplayMatchAsync(RiotAccount account, long matchId)
        {
            return Task.FromResult<Match>(null);
        }

        private async Task<Match> GetSpectatorMatchAsync(RiotAccount account, long matchId)
        {
            Match matchOutcomeAsync;
            try
            {
                ReplayConfig replay = account.Endpoints.Replay;
                SpectateClient spectateClient = new SpectateClient(account.RealmId, replay.PlatformId, ChampionNameData.NameToId, replay.SpectateUri);
                matchOutcomeAsync = await spectateClient.GetMatchOutcomeAsync(matchId);
            }
            catch
            {
                matchOutcomeAsync = null;
            }
            return matchOutcomeAsync;
        }

        private Task<Match> GetWintermintMatchAsync(RiotAccount account, long matchId)
        {
            return Task.FromResult<Match>(null);
        }

        private void NotifyMatchCompleted(Match match)
        {
            JsApiService.Push("riot:match:end-of-game", new { id = match.Id, realmId = match.RealmId, matchId = match.MatchId });
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            RiotAccount riotAccount = sender as RiotAccount;
            EndOfGameStats body = args.Body as EndOfGameStats;
            if (body != null)
            {
                Match match = MatchService.MatchTransformer.Transform(body, riotAccount.LastNonNullGame, riotAccount.RealmId);
                MatchService.GameRewards gameReward = new MatchService.GameRewards()
                {
                    InfluencePoints = (int)body.IpEarned,
                    Experience = (int)body.ExperienceEarned,
                    RiotPoints = 0
                };
                match.AdditionalData = gameReward;
                this.lastMatches[match.RealmId] = match;
                MatchService.CacheMatch(match);
                this.NotifyMatchCompleted(match);
            }
            SimpleDialogMessage simpleDialogMessage = args.Body as SimpleDialogMessage;
            if (simpleDialogMessage != null && simpleDialogMessage.Type == "leagues")
            {
                Match item = this.lastMatches[riotAccount.RealmId];
                if (item == null)
                {
                    return;
                }
                JObject jObjects = simpleDialogMessage.Params.OfType<string>().Select<string, JObject>(new Func<string, JObject>(JObject.Parse)).FirstOrDefault<JObject>((JObject x) => (long)x["gameId"] == item.MatchId);
                if (jObjects == null)
                {
                    return;
                }
                MatchService.GameRewards additionalData = item.AdditionalData as MatchService.GameRewards;
                if (additionalData == null)
                {
                    return;
                }
                additionalData.LeaguePoints = (int)jObjects["leaguePointsDelta"];
                this.NotifyMatchCompleted(item);
            }
        }

        private class GameRewards
        {
            public int InfluencePoints;

            public int RiotPoints;

            public int LeaguePoints;

            public int Experience;

            public GameRewards()
            {
            }
        }
    }
}