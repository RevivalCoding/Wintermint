using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using RiotGames.Leagues.Pojo;
using RiotGames.Platform.Gameclient.Domain;
using RiotGames.Platform.Leagues.Client.Dto;
using RiotGames.Platform.Statistics;
using RiotGames.Platform.Summoner;
using RiotGames.Platform.Summoner.Runes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Helpers;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("profile")]
    public class ProfileService : JsApiService
    {
        private const string kMainRatedQueue = "RANKED_SOLO_5x5";

        public ProfileService()
        {
        }

        [MicroApiMethod("getLeagues")]
        public async Task<object> ClientGetLeagues(dynamic args)
        {
            bool flag;
            string str = (string)args.realm;
            string str1 = (string)args.summonerName;
            PublicSummoner summoner = await JsApiService.GetSummoner(str, str1);
            ProfileService.JsLeague[] leagues = await this.GetLeagues(str, summoner.SummonerId);
            ProfileService.JsLeague[] jsLeagueArray = leagues;
            for (int i = 0; i < (int)jsLeagueArray.Length; i++)
            {
                jsLeagueArray[i].Divisions = null;
            }
            return leagues;
        }

        [MicroApiMethod("runes.available")]
        public async Task<object> GetAvailableRunes(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            long summonerId = riotAccount.SummonerId;
            SummonerRuneInventory summonerRuneInventory = await riotAccount.InvokeCachedAsync<SummonerRuneInventory>("summonerRuneService", "getSummonerRuneInventory", summonerId);
            List<SummonerRune> summonerRunes = summonerRuneInventory.SummonerRunes;
            object array = (
                from x in summonerRunes
                select new { Id = x.RuneId, Count = x.Quantity }).ToArray();
            return array;
        }

        [MicroApiMethod("getChampions")]
        public async Task<object> GetChampions(dynamic args)
        {
            string str = (string)args.realm;
            string str1 = (string)args.summonerName;
            string str2 = (string)args.season;
            PublicSummoner summoner = await JsApiService.GetSummoner(str, str1);
            object[] rankedChampions = await ProfileService.GetRankedChampions(str, summoner.AcctId, "CLASSIC", ProfileService.GetCompetitiveSeasonFromJs(str2));
            return rankedChampions;
        }

        private static string GetCompetitiveSeasonFromJs(string js)
        {
            js = js ?? string.Empty;
            string lowerInvariant = js.ToLowerInvariant();
            string str = lowerInvariant;
            if (lowerInvariant != null)
            {
                if (str == "one")
                {
                    return "ONE";
                }
                if (str == "two")
                {
                    return "TWO";
                }
            }
            return "CURRENT";
        }

        [MicroApiMethod("getDivision")]
        public async Task<object> GetDivision(dynamic args)
        {
            object obj;
            string str = (string)args.realm;
            string str1 = (string)args.summonerName;
            string str2 = (string)args.id;
            string str3 = (string)args.division;
            PublicSummoner summoner = await JsApiService.GetSummoner(str, str1);
            ProfileService.JsLeague[] leagues = await this.GetLeagues(str, summoner.SummonerId);
            ProfileService.JsLeague jsLeague = leagues.FirstOrDefault<ProfileService.JsLeague>((ProfileService.JsLeague x) => x.Id == str2);
            if (jsLeague != null)
            {
                string division = str3;
                if (division == null)
                {
                    division = jsLeague.Division;
                }
                str3 = division;
                obj = jsLeague.Divisions.FirstOrDefault<ProfileService.JsLeagueDivision>((ProfileService.JsLeagueDivision x) => x.Division == str3);
            }
            else
            {
                obj = null;
            }
            return obj;
        }

        [MicroApiMethod("getGames")]
        public async Task<object> GetGames(dynamic args)
        {
            string str = (string)args.realm;
            string str1 = (string)args.summonerName;
            PublicSummoner summoner = await JsApiService.GetSummoner(str, str1);
            return await this.GetMatchHistory(str, summoner.AccountId);
        }

        private async Task<ProfileService.JsLeague[]> GetLeagues(string realm, long summonerId)
        {
            RiotAccount riotAccount = JsApiService.AccountBag.Get(realm);
            SummonerLeaguesDTO summonerLeaguesDTO = await riotAccount.InvokeCachedAsync<SummonerLeaguesDTO>("leaguesServiceProxy", "getAllLeaguesForPlayer", summonerId);
            List<LeagueListDTO> summonerLeagues = summonerLeaguesDTO.SummonerLeagues;
            var variable = 
                from league in summonerLeagues
                select new { league = league, entries = league.Entries };
            var collection = 
                from <>h__TransparentIdentifier6 in variable
                select new { <>h__TransparentIdentifier6 = <>h__TransparentIdentifier6, me = <>h__TransparentIdentifier6.entries.FirstOrDefault<LeagueItemDTO>((LeagueItemDTO x) => x.PlayerOrTeamName == <>h__TransparentIdentifier6.league.RequestorsName) };
            IEnumerable<ProfileService.JsLeague> jsLeagues = collection.Select((argument1) => {
                Func<LeagueItemDTO, int> func = null;
                return new ProfileService.JsLeague()
                {
                    Id = string.Format("/{0}/{1}/", argument1.<>h__TransparentIdentifier6.league.Queue, argument1.<>h__TransparentIdentifier6.league.RequestorsName),
                    Name = argument1.<>h__TransparentIdentifier6.league.RequestorsName,
                    Queue = argument1.<>h__TransparentIdentifier6.league.Queue,
                    League = argument1.<>h__TransparentIdentifier6.league.Tier.ToProperCase(),
                    LeagueName = argument1.<>h__TransparentIdentifier6.league.Name,
                    Division = argument1.<>h__TransparentIdentifier6.league.RequestorsRank,
                    Divisions = (
                        from entry in argument1.<>h__TransparentIdentifier6.league.Entries
                        group entry by entry.Rank).Select<IGrouping<string, LeagueItemDTO>, ProfileService.JsLeagueDivision>((IGrouping<string, LeagueItemDTO> division) => {
                        ProfileService.JsLeagueDivision jsLeagueDivision = new ProfileService.JsLeagueDivision()
                        {
                            LeagueName = argument1.<>h__TransparentIdentifier6.league.Name,
                            League = argument1.<>h__TransparentIdentifier6.league.Tier.ToProperCase(),
                            Division = division.Key
                        };
                        ProfileService.JsLeagueDivision jsLeagueDivision1 = jsLeagueDivision;
                        IGrouping<string, LeagueItemDTO> strs = division;
                        if (func == null)
                        {
                            func = (LeagueItemDTO x) => x.LeaguePoints;
                        }
                        jsLeagueDivision1.Members = strs.OrderByDescending<LeagueItemDTO, int>(func).Select((LeagueItemDTO entry, int i) => new { Id = entry.PlayerOrTeamId, Position = i + 1, Name = entry.PlayerOrTeamName, Wins = entry.Wins, Losses = entry.Losses, Points = entry.LeaguePoints, Requestor = entry.PlayerOrTeamName == argument1.<>h__TransparentIdentifier6.league.RequestorsName });
                        return jsLeagueDivision;
                    }),
                    Points = argument1.me.LeaguePoints,
                    Wins = argument1.me.Wins,
                    Losses = argument1.me.Losses
                };
            });
            return jsLeagues.ToArray<ProfileService.JsLeague>();
        }

        private async Task<ProfileService.JsLeague[]> GetLeagueSummary(string realm, long summonerId)
        {
            bool flag;
            ProfileService.JsLeague[] leagues = await this.GetLeagues(realm, summonerId);
            ProfileService.JsLeague[] jsLeagueArray = leagues;
            for (int i = 0; i < (int)jsLeagueArray.Length; i++)
            {
                jsLeagueArray[i].Divisions = null;
            }
            return leagues;
        }

        [MicroApiMethod("getMainLeague")]
        public async Task<object> GetMainLeague(dynamic args)
        {
            long num;
            string str = (string)args.realm;
            string str1 = (string)args.summonerName;
            num = (str1 != null ? await JsApiService.GetSummoner(str, str1).SummonerId : (long)args.summonerId);
            ProfileService.JsLeague[] leagueSummary = await this.GetLeagueSummary(str, num);
            ProfileService.JsLeague unranked = ((IEnumerable<ProfileService.JsLeague>)leagueSummary).FirstOrDefault<ProfileService.JsLeague>((ProfileService.JsLeague x) => x.Queue == "RANKED_SOLO_5x5");
            if (unranked == null)
            {
                unranked = ProfileService.JsLeague.Unranked;
            }
            return unranked;
        }

        [MicroApiMethod("masteries.get")]
        public Task<object> GetMasterySetups(dynamic args)
        {
            string str = (string)args.realm;
            return InventoryHelper.GetMasterySetups(str, (string)args.summonerName);
        }

        private async Task<object> GetMatchHistory(string realm, long accountId)
        {
            RiotAccount riotAccount = JsApiService.AccountBag.Get(realm);
            GameService.JsGameMap[] maps = await GameService.GetMaps(riotAccount);
            RecentGames recentGame = await riotAccount.InvokeCachedAsync<RecentGames>("playerStatsService", "getRecentGames", accountId);
            List<PlayerGameStats> gameStatistics = recentGame.GameStatistics;
            var createDate = 
                from game in gameStatistics
                orderby game.CreateDate descending
                select new { game = game, map = maps.FirstOrDefault<GameService.JsGameMap>((GameService.JsGameMap m) => (double)m.Id == game.GameMapId) };
            var variable = 
                from <>h__TransparentIdentifier44 in createDate
                select new { <>h__TransparentIdentifier44 = <>h__TransparentIdentifier44, stats = <>h__TransparentIdentifier44.game.Statistics.ToDictionary<RawStat, string, double>((RawStat x) => x.StatType, (RawStat x) => x.Value) };
            var collection = 
                from <>h__TransparentIdentifier45 in variable
                select new { <>h__TransparentIdentifier45 = <>h__TransparentIdentifier45, win = JsApiService.GetGameStat(<>h__TransparentIdentifier45.stats, "WIN") != 0 };
            var variable1 = 
                from <>h__TransparentIdentifier46 in collection
                select new { <>h__TransparentIdentifier46 = <>h__TransparentIdentifier46, lose = JsApiService.GetGameStat(<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "LOSE") != 0 };
            object obj = 
                from <>h__TransparentIdentifier47 in variable1
                let outcome = (<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.win ? "win" : (<>h__TransparentIdentifier47.lose ? "loss" : "afk"))
                select new { RealmId = realm, MatchId = <>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.<>h__TransparentIdentifier44.game.GameId, MapId = (<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.<>h__TransparentIdentifier44.map != null ? <>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.<>h__TransparentIdentifier44.map.Id : -1), Queue = <>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.<>h__TransparentIdentifier44.game.QueueType, ChampionId = <>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.<>h__TransparentIdentifier44.game.ChampionId, Started = <>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.<>h__TransparentIdentifier44.game.CreateDate, Duration = 0, Ip = <>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.<>h__TransparentIdentifier44.game.IpEarned, Outcome = outcome, Kills = JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "CHAMPIONS_KILLED"), Deaths = JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "NUM_DEATHS"), Assists = JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "ASSISTS"), MultiKill = JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "LARGEST_MULTI_KILL"), Gold = JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "GOLD_EARNED"), Creeps = JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "MINIONS_KILLED") + JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "NEUTRAL_MINIONS_KILLED"), Spells = new double[] { <>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.<>h__TransparentIdentifier44.game.Spell1, <>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.<>h__TransparentIdentifier44.game.Spell2 }, Items = new double[] { JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "ITEM0"), JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "ITEM1"), JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "ITEM2"), JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "ITEM3"), JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "ITEM4"), JsApiService.GetGameStat(<>h__TransparentIdentifier47.<>h__TransparentIdentifier46.<>h__TransparentIdentifier45.stats, "ITEM5") } };
            return obj;
        }

        [MicroApiMethod("getOverview")]
        public async Task GetOverview(dynamic args, JsApiService.JsResponse onProgress, JsApiService.JsResponse onResult)
        {
            Func<ProfileService.JsLeague, bool> func = null;
            Func<ProfileService.JsLeague, bool> func1 = null;
            string str = (string)args.realm;
            string str1 = (string)args.summonerName;
            ConcurrentDictionary<string, object> name = new ConcurrentDictionary<string, object>();
            Action action = () => onProgress(name);
            name["summonerName"] = str1;
            action();
            PublicSummoner summoner = await JsApiService.GetSummoner(str, str1);
            if (summoner == null)
            {
                throw new JsApiException("summoner-not-found");
            }
            name["summonerName"] = summoner.Name;
            name["summonerId"] = summoner.SummonerId;
            name["accountId"] = summoner.AccountId;
            name["level"] = summoner.SummonerLevel;
            name["iconId"] = summoner.ProfileIconId;
            action();
            Task[] taskArray = new Task[] { this.GetPreviousRatings(str, summoner.AccountId).ContinueWith(JsApiService.CreatePublisher<object>(name, onProgress, "pastRatings")), this.GetLeagueSummary(str, summoner.SummonerId).ContinueWith((Task<ProfileService.JsLeague[]> task) => {
                if (task.Status != TaskStatus.RanToCompletion)
                {
                    return;
                }
                ProfileService.JsLeague[] result = task.Result;
                ConcurrentDictionary<string, object> strs = name;
                ProfileService.JsLeague[] jsLeagueArray = result;
                if (func == null)
                {
                    func = (ProfileService.JsLeague x) => x.Queue == "RANKED_SOLO_5x5";
                }
                strs["currentRating"] = ((IEnumerable<ProfileService.JsLeague>)jsLeagueArray).FirstOrDefault<ProfileService.JsLeague>(func) ?? ProfileService.JsLeague.Unranked;
                ConcurrentDictionary<string, object> strs1 = name;
                ProfileService.JsLeague[] jsLeagueArray1 = result;
                if (func1 == null)
                {
                    func1 = (ProfileService.JsLeague x) => x.Queue != "RANKED_SOLO_5x5";
                }
                strs1["otherRatings"] = ((IEnumerable<ProfileService.JsLeague>)jsLeagueArray1).Where<ProfileService.JsLeague>(func1);
                action();
            }), this.GetMatchHistory(str, summoner.AccountId).ContinueWith(JsApiService.CreatePublisher<object>(name, onProgress, "games")), ProfileService.GetRankedChampions(str, summoner.AccountId, "CLASSIC", "CURRENT").ContinueWith(JsApiService.CreatePublisher<object[]>(name, onProgress, "champions")) };
            await Task.WhenAll(taskArray);
        }

        private async Task<object> GetPreviousRatings(string realm, long accountId)
        {
            RiotAccount riotAccount = JsApiService.AccountBag.Get(realm);
            AllPublicSummonerDataDTO allPublicSummonerDataDTO = await riotAccount.InvokeCachedAsync<AllPublicSummonerDataDTO>("summonerService", "getAllPublicSummonerDataByAccount", accountId);
            BasePublicSummonerDTO summoner = allPublicSummonerDataDTO.Summoner;
            var variable = new <>f__AnonymousType1a<int, string>[] { new { Season = 2, Tier = ProfileService.GetRatingTier(summoner.SeasonTwoTier) }, new { Season = 1, Tier = ProfileService.GetRatingTier(summoner.SeasonOneTier) } };
            return variable;
        }

        private static async Task<object[]> GetRankedChampions(string realm, long accountId, string gameMode, string season)
        {
            RiotAccount riotAccount = JsApiService.AccountBag.Get(realm);
            object[] objArray = new object[] { accountId, gameMode, season };
            AggregatedStats aggregatedStat = await riotAccount.InvokeCachedAsync<AggregatedStats>("playerStatsService", "getAggregatedStats", objArray);
            List<AggregatedStat> lifetimeStatistics = aggregatedStat.LifetimeStatistics;
            IEnumerable<IGrouping<double, AggregatedStat>> championId = 
                from x in lifetimeStatistics
                group x by x.ChampionId;
            var variable = 
                from championStatistic in championId
                select new { championStatistic = championStatistic, championId = (int)championStatistic.Key };
            var collection = 
                from <>h__TransparentIdentifier25 in variable
                select new { <>h__TransparentIdentifier25 = <>h__TransparentIdentifier25, stats = <>h__TransparentIdentifier25.championStatistic.ToDictionary<AggregatedStat, string, double>((AggregatedStat x) => x.StatType, (AggregatedStat x) => x.Value) };
            var variable1 = 
                from <>h__TransparentIdentifier26 in collection
                select new { <>h__TransparentIdentifier26 = <>h__TransparentIdentifier26, games = JsApiService.GetGameStat(<>h__TransparentIdentifier26.stats, "TOTAL_SESSIONS_PLAYED") };
            var collection1 = 
                from <>h__TransparentIdentifier27 in variable1
                select new { <>h__TransparentIdentifier27 = <>h__TransparentIdentifier27, lifetimeGold = JsApiService.GetGameStat(<>h__TransparentIdentifier27.<>h__TransparentIdentifier26.stats, "TOTAL_GOLD_EARNED") };
            var variable2 = 
                from <>h__TransparentIdentifier28 in collection1
                select new { <>h__TransparentIdentifier28 = <>h__TransparentIdentifier28, lifetimeCreeps = JsApiService.GetGameStat(<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.<>h__TransparentIdentifier26.stats, "TOTAL_MINION_KILLS") + JsApiService.GetGameStat(<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.<>h__TransparentIdentifier26.stats, "TOTAL_NEUTRAL_MINIONS_KILLED") };
            var collection2 = variable2.Where((argument4) => {
                if (argument4.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.games <= 0)
                {
                    return false;
                }
                return argument4.<>h__TransparentIdentifier28.lifetimeGold > 0;
            });
            var variable3 = 
                from <>h__TransparentIdentifier29 in collection2
                select new { ChampionId = <>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.<>h__TransparentIdentifier26.<>h__TransparentIdentifier25.championId, Games = <>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.games, Wins = JsApiService.GetGameStat(<>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.<>h__TransparentIdentifier26.stats, "TOTAL_SESSIONS_WON"), Losses = JsApiService.GetGameStat(<>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.<>h__TransparentIdentifier26.stats, "TOTAL_SESSIONS_LOST"), Kills = JsApiService.GetGameStat(<>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.<>h__TransparentIdentifier26.stats, "TOTAL_CHAMPION_KILLS") / <>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.games, Deaths = JsApiService.GetGameStat(<>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.<>h__TransparentIdentifier26.stats, "TOTAL_DEATHS_PER_SESSION") / <>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.games, Assists = JsApiService.GetGameStat(<>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.<>h__TransparentIdentifier26.stats, "TOTAL_ASSISTS") / <>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.games, Gold = <>h__TransparentIdentifier29.<>h__TransparentIdentifier28.lifetimeGold / <>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.games, Creeps = <>h__TransparentIdentifier29.lifetimeCreeps / <>h__TransparentIdentifier29.<>h__TransparentIdentifier28.<>h__TransparentIdentifier27.games };
            return variable3.ToArray();
        }

        private static string GetRatingTier(string tier)
        {
            if (string.IsNullOrEmpty(tier))
            {
                return "UNRANKED";
            }
            return tier;
        }

        [MicroApiMethod("runes.get")]
        public Task<object> GetRuneSetups(dynamic args)
        {
            string str = (string)args.realm;
            return InventoryHelper.GetRuneSetups(str, (string)args.summonerName);
        }

        [MicroApiMethod("masteries.set")]
        public Task SetMasterySetups(JObject obj)
        {
            return InventoryHelper.SetMasterySetups(obj).ContinueWith((Task task) => JsApiService.Push("riot:masteries:updated", null));
        }

        [MicroApiMethod("runes.set")]
        public Task SetRuneSetups(JObject args)
        {
            string item = (string)args["realm"];
            string str = (string)args["summonerName"];
            return InventoryHelper.SetRuneSetups(item, str, args).ContinueWith((Task task) => JsApiService.Push("riot:runes:updated", null));
        }

        [Serializable]
        private class JsLeague
        {
            public string Id;

            public string LeagueName;

            public string Name;

            public string Queue;

            public string League;

            public string Division;

            public IEnumerable<ProfileService.JsLeagueDivision> Divisions;

            public int Points;

            public int Wins;

            public int Losses;

            public readonly static ProfileService.JsLeague Unranked;

            static JsLeague()
            {
                ProfileService.JsLeague.Unranked = new ProfileService.JsLeague()
                {
                    League = "Unranked"
                };
            }

            public JsLeague()
            {
            }
        }

        [Serializable]
        private class JsLeagueDivision
        {
            public string LeagueName;

            public string League;

            public string Division;

            public IEnumerable<object> Members;

            public JsLeagueDivision()
            {
            }
        }
    }
}