using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using RiotGames.Platform.Game;
using RiotGames.Platform.Game.Map;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Notification;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("game")]
    public class GameService : JsApiService
    {
        private readonly static Regex MapDisplayNameTransformer;

        static GameService()
        {
            GameService.MapDisplayNameTransformer = new Regex("^(?:The )?(.*?)!?$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        public GameService()
        {
        }

        [MicroApiMethod("getMaps")]
        public Task<GameService.JsGameMap[]> ClientGetMaps()
        {
            return GameService.GetMaps(JsApiService.RiotAccount);
        }

        [MicroApiMethod("createTutorial")]
        public async Task CreateTutorialGame(dynamic args)
        {
            int num = ((bool)args.basic ? 0 : 1);
            await JsApiService.RiotAccount.InvokeAsync<object>("gameService", "createTutorialGame", num);
        }

        [MicroApiMethod("unspectate")]
        public async Task DeclineObserverReconnect()
        {
            await JsApiService.RiotAccount.InvokeAsync<object>("gameService", "declineObserverReconnect");
        }

        [MicroApiMethod("getGameTypeConfigs")]
        public object GetGameTypeConfigs()
        {
            Func<GameTypeConfigDTO, object> variable = (GameTypeConfigDTO x) => new { Id = x.Id, Name = x.Name, TradesEnabled = x.AllowTrades, Timers = new double[] { x.BanTimerDuration, x.MainPickTimerDuration, x.PostPickTimerDuration } };
            return new { All = JsApiService.RiotAccount.GameTypeConfigs.Select<GameTypeConfigDTO, object>(variable), Practice = JsApiService.RiotAccount.PracticeGameTypeConfigs.Select<GameTypeConfigDTO, object>(variable) };
        }

        public static async Task<GameService.JsGameMap[]> GetMaps(RiotAccount account)
        {
            GameMap[] gameMapArray = await account.InvokeCachedAsync<GameMap[]>("gameMapService", "getGameMapList", TimeSpan.FromDays(1));
            GameMap[] gameMapArray1 = gameMapArray;
            IEnumerable<GameMap> mapId =
                from x in (IEnumerable<GameMap>)gameMapArray1
                where x.MapId != 4
                select x;
            IEnumerable<GameService.JsGameMap> jsGameMap =
                from x in mapId
                select new GameService.JsGameMap()
                {
                    Id = x.MapId,
                    Name = GameService.TransformMapDisplayName(x.DisplayName),
                    Players = x.TotalPlayers
                };
            GameService.JsGameMap[] array = (
                from x in jsGameMap
                orderby x.Name
                select x).ToArray<GameService.JsGameMap>();
            return array;
        }

        [MicroApiMethod("reconnect")]
        public async Task Reconnect()
        {
            PlatformGameLifecycleDTO platformGameLifecycleDTO = await JsApiService.RiotAccount.InvokeAsync<PlatformGameLifecycleDTO>("gameService", "retrieveInProgressGameInfo");
            if (platformGameLifecycleDTO != null && platformGameLifecycleDTO.PlayerCredentials != null)
            {
                GameMaestroService.StartGame(JsApiService.RiotAccount.RealmId, platformGameLifecycleDTO.PlayerCredentials);
            }
        }

        private static string TransformMapDisplayName(string mapDisplayName)
        {
            return GameService.MapDisplayNameTransformer.Match(mapDisplayName).Groups[1].Value.Trim();
        }

        [Serializable]
        public class JsGameMap
        {
            public int Id;

            public string Name;

            public int Players;

            public JsGameMap()
            {
            }
        }
    }
}