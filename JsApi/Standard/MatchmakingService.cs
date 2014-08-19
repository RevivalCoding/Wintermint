using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using RiotGames.Platform.Matchmaking;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("matchmaking")]
    public class MatchmakingService : JsApiService
    {
        public MatchmakingService()
        {
        }

        [MicroApiMethod("notifyAcceptedInviteId")]
        public async Task AcceptInviteForMatchmakingGame(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            await riotAccount.InvokeAsync<object>("matchmakerService", "acceptInviteForMatchmakingGame", (string)args.inviteId);
        }

        [MicroApiMethod("queue")]
        public async Task AttachToQueue(dynamic args)
        {
            int[] numArray;
            long[] numArray1;
            numArray = (args.queueIds != (dynamic)null ? (int[])args.queueIds : new int[0]);
            int[] numArray2 = numArray;
            numArray1 = (args.summonerIds != (dynamic)null ? (long[])args.summonerIds : new long[0]);
            long[] numArray3 = numArray1;
            RiotAccount riotAccount = JsApiService.RiotAccount;
            MatchMakerParams matchMakerParam = new MatchMakerParams()
            {
                BotDifficulty = "MEDIUM",
                InvitationId = (string)args.inviteId,
                QueueIds = numArray2.ToList<int>(),
                Team = numArray3.ToList<long>()
            };
            await riotAccount.InvokeAsync<SearchingForMatchNotification>("matchmakerService", "attachTeamToQueues", matchMakerParam);
        }

        [MicroApiMethod("getQueues")]
        public async Task<object> GetAvailableQueues()
        {
            RiotAccount riotAccount = JsApiService.AccountBag.Get(JsApiService.RiotAccount.RealmId, RiotAccountPreference.InactivePreferred);
            return await riotAccount.InvokeCachedAsync<GameQueueConfig[]>("matchmakerService", "getAvailableQueues");
        }

        [MicroApiMethod("getQueueInfo")]
        public Task<QueueInfo> GetQueueInfo(dynamic args)
        {
            int num = (int)args.queueId;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(JsApiService.RiotAccount.RealmId, RiotAccountPreference.InactivePreferred);
            return riotAccount.InvokeAsync<QueueInfo>("matchmakerService", "getQueueInfo", num);
        }

        [MicroApiMethod("acceptGame")]
        public async Task HandleFoundGame(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            await riotAccount.InvokeAsync<object>("gameService", "acceptPoppedGame", (bool)args.accept);
        }

        [MicroApiMethod("isEnabled")]
        public Task<bool> IsEnabled()
        {
            RiotAccount riotAccount = JsApiService.AccountBag.Get(JsApiService.RiotAccount.RealmId, RiotAccountPreference.InactivePreferred);
            return riotAccount.InvokeAsync<bool>("matchmakerService", "isMatchmakingEnabled");
        }

        [MicroApiMethod("unqueue")]
        public async Task Unqueue()
        {
            await JsApiService.RiotAccount.InvokeAsync<object>("matchmakerService", "purgeFromQueues");
        }
    }
}