using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using RiotGames.Platform.Game;
using RiotGames.Platform.Gameclient.Domain.Game.Trade;
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
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("heroSelect", Preload=true)]
    public class HeroSelectService : JsApiService
    {
        private int oldChampionId;

        private string oldChampionSelectionString;

        public HeroSelectService()
        {
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>((object sender, RiotAccount account) => account.MessageReceived += new EventHandler<MessageReceivedEventArgs>(this.OnMessageReceived));
            JsApiService.AccountBag.AccountRemoved += new EventHandler<RiotAccount>((object sender, RiotAccount account) => account.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(this.OnMessageReceived));
        }

        [MicroApiMethod("trade.accept")]
        public Task AcceptChampionTrade(dynamic args)
        {
            string str = (string)args.internalName;
            this.DismissTrade();
            return this.CallAttemptTradeAsync(str, true);
        }

        [MicroApiMethod("banChampion")]
        public async Task BanChampion(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            await riotAccount.InvokeAsync<object>("gameService", "banChampion", (int)args.championId);
        }

        private Task CallAttemptTradeAsync(string summonerInternalName, bool isResposne)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            GameDTO game = riotAccount.Game;
            PlayerChampionSelectionDTO playerChampionSelectionDTO = game.PlayerChampionSelections.First<PlayerChampionSelectionDTO>((PlayerChampionSelectionDTO x) => x.SummonerInternalName == summonerInternalName);
            object[] objArray = new object[] { summonerInternalName, playerChampionSelectionDTO.ChampionId, isResposne };
            return riotAccount.InvokeAsync<object>("lcdsChampionTradeService", "attemptTrade", objArray);
        }

        [MicroApiMethod("trade.cancel")]
        public Task CancelChampionTrade()
        {
            this.DismissTrade();
            return this.DeclineChampionTrade();
        }

        [MicroApiMethod("cancelSelectChampion")]
        public async Task CancelSelectChampion()
        {
            await JsApiService.RiotAccount.InvokeAsync<object>("gameService", "cancelSelectChampion");
        }

        [MicroApiMethod("trade.decline")]
        public Task DeclineChampionTrade()
        {
            this.DismissTrade();
            return JsApiService.RiotAccount.InvokeAsync<object>("lcdsChampionTradeService", "dismissTrade");
        }

        private void DismissTrade()
        {
            JsApiService.Push("game:current:trade:request", false);
        }

        [MicroApiMethod("spells.available")]
        public int[] GetAvailableSpells(dynamic args)
        {
            return GameData.GetAvailableSummonerSpells(JsApiService.RiotAccount.Game.GameMode);
        }

        private static string GetChampionSelectionsString(GameDTO game)
        {
            if (game.PlayerChampionSelections == null)
            {
                return "";
            }
            return string.Join<int>("/", 
                from x in game.PlayerChampionSelections
                select x.ChampionId);
        }

        [MicroApiMethod("getBannableChampions")]
        public async Task<object> GetChampionsForBan()
        {
            ChampionBanInfoDTO[] championBanInfoDTOArray = await JsApiService.RiotAccount.InvokeAsync<ChampionBanInfoDTO[]>("gameService", "getChampionsForBan");
            object array = (
                from x in (IEnumerable<ChampionBanInfoDTO>)championBanInfoDTOArray
                select new { Id = x.ChampionId }).ToArray();
            return array;
        }

        [MicroApiMethod("lockIn")]
        public async Task LockIn()
        {
            await JsApiService.RiotAccount.InvokeAsync<object>("gameService", "championSelectCompleted");
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            try
            {
                RiotAccount riotAccount = sender as RiotAccount;
                if (riotAccount != null && riotAccount == JsApiService.RiotAccount && riotAccount.Game != null)
                {
                    GameDTO body = args.Body as GameDTO;
                    if (body == null)
                    {
                        TradeContractDTO tradeContractDTO = args.Body as TradeContractDTO;
                        if (tradeContractDTO != null)
                        {
                            PlayerParticipant[] array = riotAccount.Game.AllPlayers.ToArray<PlayerParticipant>();
                            string summonerName = array.First<PlayerParticipant>((PlayerParticipant x) => x.SummonerInternalName == tradeContractDTO.RequesterInternalSummonerName).SummonerName;
                            string str = array.First<PlayerParticipant>((PlayerParticipant x) => x.SummonerInternalName == tradeContractDTO.RequesterInternalSummonerName).SummonerName;
                            string state = tradeContractDTO.State;
                            string str1 = state;
                            if (state != null)
                            {
                                if (str1 == "PENDING")
                                {
                                    JsApiService.Push("game:current:trade:request", true);
                                    JsApiService.Push("game:current:trade:data", new { Request = new { IsSelf = tradeContractDTO.RequesterInternalSummonerName == riotAccount.SummonerInternalName, SummonerName = summonerName, ChampionId = tradeContractDTO.RequesterChampionId }, Response = new { IsSelf = tradeContractDTO.ResponderInternalSummonerName == riotAccount.SummonerInternalName, SummonerName = str, ChampionId = tradeContractDTO.ResponderChampionId }, OtherSummonerName = (tradeContractDTO.RequesterInternalSummonerName == riotAccount.SummonerInternalName ? str : summonerName), OtherSummonerInternalName = tradeContractDTO.RequesterInternalSummonerName });
                                }
                                else if (str1 == "BUSY")
                                {
                                    this.DismissTrade();
                                    JsApiService.Push("game:current:trade:status", "busy");
                                }
                                else if (str1 == "DECLINED")
                                {
                                    this.DismissTrade();
                                    JsApiService.Push("game:current:trade:status", "declined");
                                }
                                else if (str1 == "INVALID" || str1 == "CANCELED")
                                {
                                    this.DismissTrade();
                                    JsApiService.Push("game:current:trade:request", false);
                                }
                            }
                        }
                    }
                    else
                    {
                        PlayerChampionSelectionDTO playerChampionSelectionDTO = body.PlayerChampionSelections.FirstOrDefault<PlayerChampionSelectionDTO>((PlayerChampionSelectionDTO x) => x.SummonerInternalName == riotAccount.SummonerInternalName);
                        int num = (playerChampionSelectionDTO != null ? playerChampionSelectionDTO.ChampionId : 0);
                        if (num != this.oldChampionId)
                        {
                            this.DismissTrade();
                        }
                        this.oldChampionId = num;
                        GameTypeConfigDTO gameTypeConfigDTO = riotAccount.GameTypeConfigs.First<GameTypeConfigDTO>((GameTypeConfigDTO x) => x.Id == (double)body.GameTypeConfigId);
                        if (body.GameState != "POST_CHAMP_SELECT" || !gameTypeConfigDTO.AllowTrades)
                        {
                            JsApiService.Push("game:current:trade:targets", new object[0]);
                        }
                        if (gameTypeConfigDTO.AllowTrades)
                        {
                            string championSelectionsString = HeroSelectService.GetChampionSelectionsString(body);
                            if (championSelectionsString != this.oldChampionSelectionString)
                            {
                                this.UpdateTradersAsync(riotAccount);
                            }
                            this.oldChampionSelectionString = championSelectionsString;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        [MicroApiMethod("quit")]
        public async Task QuitGame()
        {
            await JsApiService.RiotAccount.InvokeAsync<object>("gameService", "quitGame");
            JsApiService.RiotAccount.Game = null;
        }

        [MicroApiMethod("trade.request")]
        public async Task RequestChampionTrade(dynamic args)
        {
        }

        [MicroApiMethod("reroll")]
        public async Task Reroll()
        {
            await JsApiService.RiotAccount.InvokeAsync<object>("lcdsRerollService", "roll");
        }

        [MicroApiMethod("selectChampion")]
        public async Task SelectChampion(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            await riotAccount.InvokeAsync<object>("gameService", "selectChampion", (int)args.championId);
        }

        [MicroApiMethod("selectSkin")]
        public async Task SelectChampionSkin(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            object[] objArray = new object[] { (int)args.championId, (int)args.skinId };
            await riotAccount.InvokeAsync<object>("gameService", "selectChampionSkin", objArray);
        }

        [MicroApiMethod("spells.select")]
        public async Task SelectSummoners(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            object[] objArray = new object[] { (int)args.spell1, (int)args.spell2 };
            await riotAccount.InvokeAsync<object>("gameService", "selectSpells", objArray);
        }

        [MicroApiMethod("setMasteryPage")]
        public Task SetMasteryPage(JObject setup)
        {
            return InventoryHelper.SetActiveMasterySetup(setup);
        }

        [MicroApiMethod("setRunePage")]
        public Task SetRunePage(JObject setup)
        {
            return InventoryHelper.SetActiveRuneSetup(JsApiService.RiotAccount.RealmId, JsApiService.RiotAccount.SummonerName, setup);
        }

        private async void UpdateTradersAsync(RiotAccount account)
        {
            try
            {
                PotentialTradersDTO potentialTradersDTO = await account.InvokeAsync<PotentialTradersDTO>("lcdsChampionTradeService", "getPotentialTraders");
                if (account == JsApiService.RiotAccount)
                {
                    IEnumerable<PlayerParticipant> allPlayers = account.Game.AllPlayers;
                    JsApiService.JsPush push = JsApiService.Push;
                    var potentialTraders = 
                        from traderInternalName in potentialTradersDTO.PotentialTraders
                        select new { traderInternalName = traderInternalName, player = allPlayers.First<PlayerParticipant>((PlayerParticipant player) => player.SummonerInternalName == traderInternalName) };
                    push("game:current:trade:targets", 
                        from <>h__TransparentIdentifier4a in potentialTraders
                        select <>h__TransparentIdentifier4a.player.SummonerId);
                }
                else
                {
                    return;
                }
            }
            catch (Exception exception)
            {
            }
        }
    }
}