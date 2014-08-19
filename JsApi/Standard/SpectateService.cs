using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using RiotGames.Platform.Game;
using RiotGames.Platform.Gameclient.Domain;
using RiotGames.Platform.Summoner;
using RiotSpectate.Spectate;
using RiotSpectate.Spectate.RiotDto;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.Data;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Helpers;
using WintermintClient.JsApi.Notification;
using WintermintClient.Riot;
using WintermintData.Helpers.Matches;
using WintermintData.Matches;
using WintermintData.Riot.Account;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("spectate")]
    public class SpectateService : JsApiService
    {
        public SpectateService()
        {
        }

        [MicroApiMethod("find")]
        public async Task<SpectateService.JsSpectatorThing> FindGame(dynamic args)
        {
            SpectateService.JsSpectatorThing jsSpectatorThing;
            string str = (string)args.realm;
            string str1 = (string)args.summonerName;
            PublicSummoner summoner = await JsApiService.GetSummoner(str, str1);
            if (summoner != null)
            {
                try
                {
                    RiotAccount riotAccount = JsApiService.AccountBag.Get(str);
                    PlatformGameLifecycleDTO spectatorGameThrowable = await SpectateService.GetSpectatorGameThrowable(str, str1);
                    jsSpectatorThing = new SpectateService.JsSpectatorThing("in-game", spectatorGameThrowable.Game, riotAccount.RealmId, summoner.SummonerId, TimeSpan.FromSeconds((double)spectatorGameThrowable.ReconnectDelay));
                }
                catch (InvocationException invocationException)
                {
                    string faultString = invocationException.FaultString;
                    if (faultString == null)
                    {
                        faultString = "";
                    }
                    string lowerInvariant = faultString.ToLowerInvariant();
                    if (!lowerInvariant.Contains("not started"))
                    {
                        jsSpectatorThing = (!lowerInvariant.Contains("not observable") ? new SpectateService.JsSpectatorThing("out-of-game") : new SpectateService.JsSpectatorThing("observer-disabled"));
                    }
                    else
                    {
                        jsSpectatorThing = new SpectateService.JsSpectatorThing("game-assigned");
                    }
                }
                catch
                {
                    jsSpectatorThing = new SpectateService.JsSpectatorThing("error");
                }
            }
            else
            {
                jsSpectatorThing = new SpectateService.JsSpectatorThing("no-summoner");
            }
            return jsSpectatorThing;
        }

        [MicroApiMethod("findMore")]
        public async Task<object> FindMore(dynamic args)
        {
            string str = (string)args.realm;
            long num = (long)args.matchId;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(str);
            SpectateClient spectateClient = new SpectateClient(riotAccount.RealmId, riotAccount.Endpoints.Replay.PlatformId, ChampionNameData.NameToId, riotAccount.Endpoints.Replay.SpectateUri);
            GameDescription gameDescriptionAsync = await spectateClient.GetGameDescriptionAsync(num);
            object variable = new { Started = gameDescriptionAsync.StartTime, Featured = gameDescriptionAsync.FeaturedGame, Elo = gameDescriptionAsync.InterestScore, Ended = gameDescriptionAsync.GameEnded };
            return variable;
        }

        public static async Task<PlatformGameLifecycleDTO> GetSpectatorGame(string realmId, string summonerName)
        {
            PlatformGameLifecycleDTO spectatorGameThrowable;
            try
            {
                spectatorGameThrowable = await SpectateService.GetSpectatorGameThrowable(realmId, summonerName);
                return spectatorGameThrowable;
            }
            catch
            {
            }
            spectatorGameThrowable = null;
            return spectatorGameThrowable;
        }

        public static Task<PlatformGameLifecycleDTO> GetSpectatorGameThrowable(string realmId, string summonerName)
        {
            RiotAccount riotAccount = JsApiService.AccountBag.Get(realmId);
            return riotAccount.InvokeCachedAsync<PlatformGameLifecycleDTO>("gameService", "retrieveInProgressSpectatorGameInfo", summonerName, TimeSpan.FromSeconds(5));
        }

        [MicroApiMethod("spectate")]
        public async Task SpectateGame(dynamic args)
        {
            string str = (string)args.realm;
            string str1 = (string)args.summonerName;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(str);
            PlatformGameLifecycleDTO spectatorGame = await SpectateService.GetSpectatorGame(str, str1);
            if (spectatorGame == null)
            {
                throw new JsApiException("no-game");
            }
            await GameMaestroService.TryStartSpectatorGame(str, riotAccount.Endpoints.Replay.PlatformId, spectatorGame.PlayerCredentials);
        }

        public class JsSpectatorThing
        {
            public object Status;

            public object Game;

            public object Dude;

            public object StandardSpectateBegins;

            public JsSpectatorThing(string status)
            {
                this.Status = status;
            }

            public JsSpectatorThing(string status, GameDTO game, string realmId, long summonerId, TimeSpan observerDelay)
            {
                Match match = InGameMatchTransformer.Transform(game, realmId);
                this.Status = status;
                this.Game = match;
                this.Dude = ((IEnumerable<Team>)match.Teams).SelectMany<Team, TeamMember>((Team x) => x.Members).First<TeamMember>((TeamMember x) =>
                {
                    long? nullable = x.SummonerId;
                    long num = summonerId;
                    if (nullable.GetValueOrDefault() != num)
                    {
                        return false;
                    }
                    return nullable.HasValue;
                });
                this.StandardSpectateBegins = DateTime.UtcNow + observerDelay;
            }
        }
    }
}