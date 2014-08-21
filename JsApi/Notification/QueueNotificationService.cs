using MicroApi;
using RiotGames.Platform.Game;
using RiotGames.Platform.Game.Message;
using RiotGames.Platform.Matchmaking;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Notification
{
    [MicroApiSingleton]
    public class QueueNotificationService : JsApiService
    {
        private const int kNoQueueId = -1;

        private const int kStatusAfk = 0;

        private const int kStatusAccept = 1;

        private const int kStatusDecline = 2;

        public QueueNotificationService()
        {
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                account.Storage["queueId"] = -1;
                account.Blockers["queue"] = () =>
                {
                    if ((int)account.Storage["queueId"] == -1)
                    {
                        return null;
                    }
                    return "in-queue";
                };
                account.InvocationResult += new EventHandler<InvocationResultEventArgs>(this.OnInvocationResult);
                account.MessageReceived += new EventHandler<MessageReceivedEventArgs>(this.OnFlexMessageReceived);
            });
            JsApiService.AccountBag.AccountRemoved += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                account.InvocationResult -= new EventHandler<InvocationResultEventArgs>(this.OnInvocationResult);
                account.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(this.OnFlexMessageReceived);
            });
        }

        private static IEnumerable<string> GetAcceptDeclineStatus(string statusOfParticipants)
        {
            char[] charArray = statusOfParticipants.ToCharArray();
            return charArray.Select<char, string>(new Func<char, string>(QueueNotificationService.GetSingleStatus));
        }

        private static string GetSingleStatus(char status)
        {
            switch (status)
            {
                case '0':
                    {
                        return "none";
                    }
                case '1':
                    {
                        return "accepted";
                    }
                case '2':
                    {
                        return "declined";
                    }
            }
            return "unknown";
        }

        private async void OnData(RiotAccount account, object obj)
        {
            try
            {
                await this.ProcessData(account, obj);
            }
            catch
            {
            }
        }

        private void OnFlexMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            this.OnData(sender as RiotAccount, args.Body);
        }

        private void OnInvocationResult(object sender, InvocationResultEventArgs args)
        {
            RiotAccount riotAccount = sender as RiotAccount;
            this.OnData(riotAccount, args.Result);
            if (args.Service == "matchmakerService" && args.Method == "purgeFromQueues" && args.Success)
            {
                this.SetLeftQueue(riotAccount);
                return;
            }
            if (args.Service == "gameService" && args.Method == "quitGame" && riotAccount.Game != null && JsApiService.IsGameStateExitable(riotAccount.Game.GameState))
            {
                this.SetLeftQueue(riotAccount);
            }
        }

        private async Task ProcessData(RiotAccount account, object obj)
        {
            bool flag;
            object[] objArray;
            long num2;
            SearchingForMatchNotification searchingForMatchNotification = obj as SearchingForMatchNotification;
            if (searchingForMatchNotification == null)
            {
                GameNotification gameNotification = obj as GameNotification;
                if (gameNotification == null)
                {
                    GameDTO gameDTO = obj as GameDTO;
                    if (gameDTO != null && gameDTO.StatusOfParticipants != null)
                    {
                        char[] charArray = gameDTO.StatusOfParticipants.ToCharArray();
                        int[] array = (
                            from x in (IEnumerable<char>)charArray
                            select int.Parse(x.ToString(CultureInfo.InvariantCulture))).ToArray<int>();
                        if (gameDTO.GameState == "START_REQUESTED" || gameDTO.GameState == "IN_PROGRESS")
                        {
                            JsApiService.PushIfActive(account, "game:queue:done", null);
                        }
                        if (gameDTO.GameState != "JOINING_CHAMP_SELECT")
                        {
                            JsApiService.PushIfActive(account, "game:queue:dropped", null);
                        }
                        string gameState = gameDTO.GameState;
                        string str = gameState;
                        if (gameState != null)
                        {
                            if (str == "JOINING_CHAMP_SELECT")
                            {
                                JsApiService.PushIfActive(account, "game:queue:found", QueueNotificationService.GetAcceptDeclineStatus(gameDTO.StatusOfParticipants));
                            }
                            else if (str == "FAILED_TO_START" || str == "START_REQUESTED" || str == "IN_PROGRESS")
                            {
                                this.SetLeftQueue(account);
                            }
                            else if (str == "TERMINATED")
                            {
                                int num3 = 0;
                                var collection = gameDTO.TeamOne.Concat<IParticipant>(gameDTO.TeamTwo).Select((IParticipant x) => {
                                    int[] cSu0024u003cu003e8_locals25 = array;
                                    int num = num3;
                                    int num1 = num;
                                    num3 = num + 1;
                                    return new { Participant = x as PlayerParticipant, Status = cSu0024u003cu003e8_locals25[num1] };
                                });
                                var array1 = (
                                    from x in collection
                                    where x.Participant != null
                                    select x).ToArray();
                                var variable1 = array1;
                                var variable2 = "";
                                variable2 = null;
                                var variable2 = ((IEnumerable<<>f__AnonymousType14<PlayerParticipant, int>>)variable1).FirstOrDefault((x) => x.Participant.SummonerId == (double)account.SummonerId);
                                if (variable2 != null)
                                {
                                    var variable3 = array1;
                                    var hasValue = 
                                        from x in (IEnumerable<<>f__AnonymousType14<PlayerParticipant, int>>)variable3
                                        where x.Participant.TeamParticipantId.HasValue
                                        select x;
                                    var teamParticipantId = 
                                        from x in hasValue
                                        group x by x.Participant.TeamParticipantId;
                                    var list = 
                                        from x in teamParticipantId
                                        select x.ToList();
                                    var lists = list;
                                    var collection1 = lists.FirstOrDefault((g) => g.Any((x) => x.Participant.SummonerId == (double)account.SummonerId));
                                    if (collection1 != null)
                                    {
                                        var collection2 = collection1;
                                        if (!collection2.Any((x) => x.Status != 1))
                                        {
                                            goto Label2;
                                        }
                                        this.SetLeftQueue(account);
                                        var collection3 = collection1;
                                        var status = 
                                            from x in collection3
                                            where x.Status != 1
                                            select x;
                                        IEnumerable<string> summonerName = 
                                            from x in status
                                            select x.Participant.SummonerName;
                                        JsApiService.PushIfActive(account, "game:queue:acceptFail", summonerName);
                                        goto Label1;
                                    }
                                Label2:
                                   
                                    if (variable2.Status != 1)
                                    {
                                        this.SetLeftQueue(account);
                                        RiotAccount riotAccount = account;
                                        string[] strArrays = new string[] { account.SummonerName };
                                        JsApiService.PushIfActive(riotAccount, "game:queue:acceptFail", strArrays);
                                    }
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                    //Label1;
                    }
                }
                else
                {
                    string type = gameNotification.Type;
                    string str1 = type;
                    if (type != null && (str1 == "PLAYER_QUIT" || str1 == "TEAM_REMOVED" || str1 == "PLAYER_REMOVED"))
                    {
                        string summonerNameBySummonerId = null;
                        if (long.TryParse(gameNotification.MessageArgument, out num2))
                        {
                            summonerNameBySummonerId = await JsApiService.GetSummonerNameBySummonerId(account.RealmId, num2);
                        }
                        this.SetLeftQueue(account);
                        if (!string.IsNullOrEmpty(summonerNameBySummonerId))
                        {
                            JsApiService.PushIfActive(account, "game:queue:leave", summonerNameBySummonerId);
                        }
                    }
                }
            }
            else if (account == JsApiService.RiotAccount)
            {
                flag = (searchingForMatchNotification.JoinedQueues == null ? false : searchingForMatchNotification.JoinedQueues.Count > 0);
                if (flag)
                {
                    QueueInfo queueInfo = searchingForMatchNotification.JoinedQueues.First<QueueInfo>();
                    this.SetEnteredQueue(account, queueInfo.QueueId);
                }
                List<QueueInfo> joinedQueues = searchingForMatchNotification.JoinedQueues;
                IEnumerable<int> queueId = 
                    from x in joinedQueues
                    select x.QueueId;
                objArray = (searchingForMatchNotification.PlayerJoinFailures != null ? searchingForMatchNotification.PlayerJoinFailures.Select<FailedJoinPlayer, object>(new Func<FailedJoinPlayer, object>(RiotJsTransformer.TransformFailedJoinPlayer)).ToArray<object>() : new object[0]);
                var variable4 = new { JoinedQueues = queueId, JoinFailures = objArray };
                JsApiService.PushIfActive(account, "game:queue:joinStatus", variable4);
            }
            else
            {
                account.InvokeAsync<object>("matchmakerService", "purgeFromQueues");
            }
        }

        private void SetEnteredQueue(RiotAccount account, int queueId)
        {
            account.Storage["queueId"] = queueId;
            JsApiService.PushIfActive(account, "game:queue", queueId);
        }

        private void SetLeftQueue(RiotAccount account)
        {
            account.Storage["queueId"] = -1;
            JsApiService.PushIfActive(account, "game:queue", -1);
            JsApiService.PushIfActive(account, "game:queue:done", null);
        }
    }
}