using Browser.Rpc;
using MicroApi;
using RiotGames.Platform.Clientfacade.Domain;
using RiotGames.Platform.Game;
using RiotGames.Platform.Game.Message;
using RiotGames.Platform.Statistics;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Standard;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Notification
{
    [MicroApiSingleton]
    public class GameNotificationService : JsApiService
    {
        private string lastGameJson;

        private string lastGameState;

        private int lastPickTurn;

        private DateTime lastTurnEnds;

        private int lastTurnDuration;

        public GameNotificationService()
        {
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                account.Blockers["game"] = () =>
                {
                    if (account.Game == null)
                    {
                        return null;
                    }
                    return "in-game";
                };
                account.MessageReceived += new EventHandler<MessageReceivedEventArgs>(this.OnFlexMessageReceived);
                account.InvocationResult += new EventHandler<InvocationResultEventArgs>(this.OnInvocationResult);
                account.Connected += new EventHandler(this.OnAccountConnected);
            });
            JsApiService.AccountBag.AccountRemoved += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                account.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(this.OnFlexMessageReceived);
                account.InvocationResult -= new EventHandler<InvocationResultEventArgs>(this.OnInvocationResult);
                account.Connected -= new EventHandler(this.OnAccountConnected);
            });
            JsApiService.AccountBag.ActiveChanged += new EventHandler<RiotAccount>(this.OnActiveAccountChanged);
        }

        private void CompleteJsGame(RiotAccount account, GameDTO game, RiotJsTransformer.JavascriptyGame jsGame)
        {
            if (game == null)
            {
                return;
            }
            GameTypeConfigDTO gameTypeConfigDTO = account.GameTypeConfigs.FirstOrDefault<GameTypeConfigDTO>((GameTypeConfigDTO x) => x.Id == (double)game.GameTypeConfigId);
            if (gameTypeConfigDTO == null)
            {
                return;
            }
            if (this.lastGameState != game.GameState || this.lastPickTurn != game.PickTurn)
            {
                this.lastGameState = game.GameState;
                this.lastPickTurn = game.PickTurn;
                string heroSelectState = jsGame.HeroSelectState;
                string str = heroSelectState;
                if (heroSelectState != null)
                {
                    if (str == "pre")
                    {
                        this.lastTurnDuration = (int)gameTypeConfigDTO.BanTimerDuration;
                        goto Label0;
                    }
                    else if (str == "pick")
                    {
                        this.lastTurnDuration = (int)gameTypeConfigDTO.MainPickTimerDuration;
                        goto Label0;
                    }
                    else
                    {
                        if (str != "post")
                        {
                            goto Label2;
                        }
                        this.lastTurnDuration = (int)gameTypeConfigDTO.PostPickTimerDuration;
                        goto Label0;
                    }
                }
            Label2:
                this.lastTurnDuration = 0;
            Label0:
                this.lastTurnEnds = DateTime.UtcNow + TimeSpan.FromSeconds((double)this.lastTurnDuration);
            }
            jsGame.TurnDuration = this.lastTurnDuration;
            jsGame.TurnEnds = this.lastTurnEnds;
        }

        private async Task GetFullGameAsync(RiotAccount account)
        {
            if (account.Game != null)
            {
                PlatformGameLifecycleDTO spectatorGame = await SpectateService.GetSpectatorGame(account.RealmId, account.SummonerName);
                PlatformGameLifecycleDTO platformGameLifecycleDTO = spectatorGame;
                GameDTO game = account.Game;
                if (game != null && platformGameLifecycleDTO.Game != null)
                {
                    game.PlayerChampionSelections = platformGameLifecycleDTO.Game.PlayerChampionSelections;
                    this.UpdateGame(account, game);
                }
            }
        }

        private static bool IsChampSelect(GameDTO game)
        {
            if (game == null)
            {
                return false;
            }
            string gameState = game.GameState;
            string str = gameState;
            if (gameState != null && (str == "PRE_CHAMP_SELECT" || str == "CHAMP_SELECT" || str == "POST_CHAMP_SELECT"))
            {
                return true;
            }
            return false;
        }

        private static bool IsGameInProgressStrict(GameDTO game)
        {
            if (game == null)
            {
                return false;
            }
            return game.GameState == "IN_PROGRESS";
        }

        private static bool IsGameTerminated(GameDTO game)
        {
            if (game == null)
            {
                return true;
            }
            string gameState = game.GameState;
            string str = gameState;
            if (gameState != null && (str == "FAILED_TO_START" || str == "TERMINATED" || str == "TERMINATED_IN_ERROR"))
            {
                return true;
            }
            return false;
        }

        private void NotifyGameChanged(RiotAccount account, GameDTO game)
        {
            RiotJsTransformer.JavascriptyGame javascriptyGame = RiotJsTransformer.TransformGame(game, account);
            this.CompleteJsGame(account, game, javascriptyGame);
            string str = PushNotification.Serialize(javascriptyGame);
            if (str == this.lastGameJson)
            {
                return;
            }
            this.lastGameJson = str;
            JsApiService.PushJson("game:current", str);
        }

        private void OnAccountConnected(object sender, EventArgs args)
        {
            RiotAccount riotAccount = sender as RiotAccount;
            if (riotAccount == null)
            {
                return;
            }
            riotAccount.InvokeAsync<object>("gameService", "retrieveInProgressGameInfo");
        }

        private void OnActiveAccountChanged(object sender, RiotAccount account)
        {
            if (account == null)
            {
                return;
            }
            this.NotifyGameChanged(account, account.Game);
        }

        private void OnData(RiotAccount account, object message)
        {
            GameDTO gameDTO = message as GameDTO;
            if (gameDTO != null)
            {
                this.UpdateGame(account, gameDTO);
                return;
            }
            EndOfGameStats endOfGameStat = message as EndOfGameStats;
            if (endOfGameStat != null)
            {
                this.UpdateGame(account, null);
                JsApiService.PushIfActive(account, "game:stats", endOfGameStat);
                return;
            }
            PlayerCredentialsDto playerCredentialsDto = message as PlayerCredentialsDto;
            if (playerCredentialsDto != null)
            {
                GameDTO game = account.Game;
                if (game == null)
                {
                    return;
                }
                game.GameState = "IN_PROGRESS";
                game.GameStateString = "IN_PROGRESS";
                this.UpdateGame(account, game);
                JsApiService.PushIfActive(account, "game:launch", playerCredentialsDto);
                return;
            }
            GameNotification gameNotification = message as GameNotification;
            if (gameNotification != null)
            {
                this.UpdateGame(account, null);
                if (gameNotification.Type == "PLAYER_BANNED_FROM_GAME")
                {
                    JsApiService.PushIfActive(account, "game:banned", null);
                }
                return;
            }
            LoginDataPacket loginDataPacket = message as LoginDataPacket;
            if (loginDataPacket != null && loginDataPacket.ReconnectInfo != null && loginDataPacket.ReconnectInfo.Game != null)
            {
                this.UpdateGame(account, loginDataPacket.ReconnectInfo.Game);
            }
        }

        private void OnFlexMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            this.OnData(sender as RiotAccount, e.Body);
        }

        private void OnInvocationResult(object sender, InvocationResultEventArgs args)
        {
            GameDTO game;
            RiotAccount riotAccount = sender as RiotAccount;
            if ((!args.Success || !(args.Service == "gameService") ? false : args.Method == "quitGame"))
            {
                if (riotAccount.Game != null && !JsApiService.IsGameStateExitable(riotAccount.Game.GameState))
                {
                    return;
                }
                this.UpdateGame(riotAccount, null);
                return;
            }
            if ((!args.Success || !(args.Service == "gameService") ? true : args.Method != "retrieveInProgressGameInfo"))
            {
                this.OnData(riotAccount, args.Result);
                return;
            }
            PlatformGameLifecycleDTO result = args.Result as PlatformGameLifecycleDTO;
            RiotAccount riotAccount1 = riotAccount;
            if (result != null)
            {
                game = result.Game;
            }
            else
            {
                game = null;
            }
            this.UpdateGame(riotAccount1, game);
        }

        private void UpdateGame(RiotAccount account, GameDTO game)
        {
            GameDTO gameDTO;
            GameDTO gameDTO1 = JsApiService.RiotAccount.Game;
            if (GameNotificationService.IsGameTerminated(game))
            {
                gameDTO = null;
            }
            else
            {
                gameDTO = game;
            }
            GameDTO gameDTO2 = gameDTO;
            account.Game = gameDTO2;
            if (JsApiService.AccountBag.Active != account)
            {
                if (gameDTO2 != null)
                {
                    account.InvokeAsync<object>("gameService", "quitGame");
                }
                return;
            }
            if (!GameNotificationService.IsChampSelect(gameDTO1) && GameNotificationService.IsChampSelect(gameDTO2))
            {
                object[] id = new object[] { game.Id, "CHAMP_SELECT_CLIENT" };
                account.InvokeAsync<object>("gameService", "setClientReceivedGameMessage", id);
            }
            if (!GameNotificationService.IsGameInProgressStrict(gameDTO1) && GameNotificationService.IsGameInProgressStrict(gameDTO2))
            {
                this.GetFullGameAsync(account);
            }
            this.NotifyGameChanged(account, game);
        }
    }
}