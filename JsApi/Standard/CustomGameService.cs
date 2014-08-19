using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using RiotGames.Platform.Game;
using RiotGames.Platform.Game.Map;
using RiotGames.Platform.Game.Practice;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.Data;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("customGame")]
    public class CustomGameService : GameJsApiService
    {
        public CustomGameService()
        {
        }

        public async Task AddBot(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            object[] objArray = new object[] { (int)args.championId, new BotParticipant() };
            await riotAccount.InvokeAsync<object>("gameService", "selectBotChampion", objArray);
        }

        [MicroApiMethod("ban")]
        public async Task BanPlayer(dynamic args)
        {
            long num = (long)args.accountId;
            GameDTO game = JsApiService.RiotAccount.Game;
            if (game.Observers.Any<GameObserver>((GameObserver x) => x.AccountId == (double)num))
            {
                RiotAccount riotAccount = JsApiService.RiotAccount;
                object[] id = new object[] { game.Id, null };
                id[1] = (long)args.accountId;
                await riotAccount.InvokeAsync<object>("gameService", "banObserverFromGame", id);
            }
            else if (game.AllPlayers.Any<PlayerParticipant>((PlayerParticipant x) => x.AccountId == (double)num))
            {
                RiotAccount riotAccount1 = JsApiService.RiotAccount;
                object[] objArray = new object[] { game.Id, null };
                objArray[1] = (long)args.accountId;
                await riotAccount1.InvokeAsync<object>("gameService", "banUserFromGame", objArray);
            }
        }

        [MicroApiMethod("create")]
        public async Task CreateGame(dynamic args)
        {
            string allowSpectators = GameJsApiService.GetAllowSpectators((string)args.spectators);
            int num = (int)args.mapId;
            string str = (string)args.name;
            string str1 = (string)args.password;
            int num1 = (int)args.gctId;
            int num2 = (int)args.players;
            RiotAccount riotAccount = JsApiService.RiotAccount;
            PracticeGameConfig practiceGameConfig = new PracticeGameConfig()
            {
                AllowSpectators = allowSpectators
            };
            PracticeGameConfig practiceGameConfig1 = practiceGameConfig;
            GameMap gameMap = new GameMap()
            {
                MapId = num,
                TotalPlayers = num2
            };
            practiceGameConfig1.GameMap = gameMap;
            practiceGameConfig.GameMode = GameJsApiService.GetGameMode(num);
            practiceGameConfig.GameName = str;
            practiceGameConfig.GamePassword = str1;
            practiceGameConfig.GameTypeConfig = num1;
            practiceGameConfig.MaxNumPlayers = num2;
            await riotAccount.InvokeAsync<object>("gameService", "createPracticeGame", practiceGameConfig);
        }

        [MicroApiMethod("list")]
        public async Task<object> GetGamesAsync(dynamic args)
        {
            string str = (string)args.realmId;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(str);
            PracticeGameSearchResult[] practiceGameSearchResultArray = await riotAccount.InvokeAsync<PracticeGameSearchResult[]>("gameService", "listAllPracticeGames");
            var variable =
                from game in (IEnumerable<PracticeGameSearchResult>)practiceGameSearchResultArray
                select new { Id = game.Id, Name = game.Name, MapId = game.GameMapId, MapName = GameJsApiService.GetGameMapFriendlyName(game.GameMapId), Mode = game.GameMode, Players = game.Team1Count + game.Team2Count, MaxPlayers = game.MaxNumPlayers, HasPassword = game.PrivateGame, IsDefault = (!PracticeGameData.IsDefaultGame(game.Name) ? false : !game.PrivateGame), Spectators = game.SpectatorCount, Host = game.Owner.SummonerName };
            return variable.ToArray();
        }

        [MicroApiMethod("join")]
        public async Task JoinGame(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            object[] objArray = new object[] { (int)args.gameId, (string)args.password };
            await riotAccount.InvokeAsync<object>("gameService", "joinGame", objArray);
        }

        [MicroApiMethod("observe")]
        public async Task ObserveGame(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            object[] objArray = new object[] { (int)args.gameId, (string)args.password };
            await riotAccount.InvokeAsync<object>("gameService", "observeGame", objArray);
        }

        [MicroApiMethod("quit")]
        public async Task QuitGame()
        {
            await JsApiService.RiotAccount.InvokeAsync<object>("gameService", "quitGame");
            JsApiService.RiotAccount.Game = null;
        }

        public async Task RemoveBot(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            object[] objArray = new object[] { (int)args.championId, new BotParticipant() };
            await riotAccount.InvokeAsync<object>("gameService", "removeBotChampion", objArray);
        }

        [MicroApiMethod("start")]
        public async Task<object> StartChampionSelect(dynamic args)
        {
            GameDTO game = JsApiService.RiotAccount.Game;
            RiotAccount riotAccount = JsApiService.RiotAccount;
            object[] id = new object[] { game.Id, game.OptimisticLock };
            StartChampSelectDTO startChampSelectDTO = await riotAccount.InvokeAsync<StartChampSelectDTO>("gameService", "startChampionSelection", id);
            object variable = new { Success = startChampSelectDTO.InvalidPlayers.Count == 0, JoinFailures = startChampSelectDTO.InvalidPlayers.Select<FailedJoinPlayer, object>(new Func<FailedJoinPlayer, object>(RiotJsTransformer.TransformFailedJoinPlayer)).ToArray<object>() };
            return variable;
        }

        [MicroApiMethod("switch")]
        public async Task SwitchTeams(dynamic args)
        {
            await JsApiService.RiotAccount.InvokeAsync<object>("gameService", "switchTeams", JsApiService.RiotAccount.Game.Id);
        }

        [MicroApiMethod("makeObserver")]
        public async Task SwitchToObserver(dynamic args)
        {
            await JsApiService.RiotAccount.InvokeAsync<object>("gameService", "switchPlayerToObserver", JsApiService.RiotAccount.Game.Id);
        }

        [MicroApiMethod("makePlayer")]
        public async Task SwitchToPlayer(dynamic args)
        {
            object obj;
            object obj1;
            double id = JsApiService.RiotAccount.Game.Id;
            dynamic obj2 = args.blue != (dynamic)null;
            obj = (!obj2 ? obj2 : obj2 & (bool)args.blue);
            dynamic obj3 = obj;
            dynamic obj4 = args.purple != (dynamic)null;
            obj1 = (!obj4 ? obj4 : obj4 & (bool)args.purple);
            dynamic obj5 = obj1;
            dynamic obj6 = obj3;
            if ((obj6 ? obj6 : obj6 | obj5))
            {
                int num = (obj3 ? 100 : 200);
                RiotAccount riotAccount = JsApiService.RiotAccount;
                object[] objArray = new object[] { id, num };
                await riotAccount.InvokeAsync<object>("gameService", "switchObserverToPlayer", objArray);
            }
        }
    }
}