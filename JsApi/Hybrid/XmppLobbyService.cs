using Chat;
using Complete.Hex;
using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using RiotGames.Platform.Game;
using RiotGames.Platform.Gameclient.Domain;
using RiotGames.Platform.Matchmaking;
using RiotGames.Platform.Summoner;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Helpers;
using WintermintClient.JsApi.Notification;
using WintermintClient.JsApi.Standard;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Hybrid
{
    [MicroApiService("lobby", Preload = true)]
    public class XmppLobbyService : JsApiService
    {
        private readonly object lobbySyncObject;

        private readonly HashSet<XmppLobbyService.LobbyMember> members;

        private readonly HashSet<XmppLobbyService.LobbyMember> invitations;

        private RiotAccount account;

        private XmppLobbyService.Role role;

        private string inviteId;

        private int queueId;

        private int maxMembers;

        private bool autoAcceptInvitation;

        private XmppLobbyService.InvitationPassbackData respondStateObject;

        private long hostSummonerId;

        private string hostSummonerName;

        public XmppLobbyService()
        {
            this.invitations = new HashSet<XmppLobbyService.LobbyMember>(XmppLobbyService.LobbyMemberComparer.Instance);
            this.members = new HashSet<XmppLobbyService.LobbyMember>(XmppLobbyService.LobbyMemberComparer.Instance);
            this.lobbySyncObject = new object();
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                account.Chat.MailReceived += new EventHandler<Chat.MessageReceivedEventArgs>(this.OnMailReceived);
                account.Chat.MessageReceived += new EventHandler<Chat.MessageReceivedEventArgs>(this.OnMessageReceived);
                account.MessageReceived += new EventHandler<RtmpSharp.Messaging.MessageReceivedEventArgs>(this.OnFlexMessageReceived);
                account.InvocationResult += new EventHandler<InvocationResultEventArgs>(this.OnInvocationResult);
                account.StateChanged += new EventHandler<StateChangedEventArgs>(this.OnAccountStateChanged);
            });
            JsApiService.AccountBag.AccountRemoved += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                account.Chat.MailReceived -= new EventHandler<Chat.MessageReceivedEventArgs>(this.OnMailReceived);
                account.Chat.MessageReceived -= new EventHandler<Chat.MessageReceivedEventArgs>(this.OnMessageReceived);
                account.MessageReceived -= new EventHandler<RtmpSharp.Messaging.MessageReceivedEventArgs>(this.OnFlexMessageReceived);
                account.InvocationResult -= new EventHandler<InvocationResultEventArgs>(this.OnInvocationResult);
                account.StateChanged -= new EventHandler<StateChangedEventArgs>(this.OnAccountStateChanged);
            });
            JsApiService.AccountBag.ActiveChanged += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                if (account.State == ConnectionState.Connected)
                {
                    this.CreateLobby(this.queueId);
                }
            });
        }

        [MicroApiMethod("create")]
        public async Task Create(dynamic args)
        {
            XmppLobbyService.Role role;
            string str = (string)args.inviteId;
            if (str == null)
            {
                Guid guid = Guid.NewGuid();
                str = string.Format("wm-{0}", guid.ToString("N"));
            }
            string str1 = str;
            if (args.role != (dynamic)null)
            {
                role = (XmppLobbyService.Role)((int)args.role);
            }
            else
            {
                role = XmppLobbyService.Role.Host;
            }
            XmppLobbyService.Role role1 = role;
            int num = (int)args.queueId;
            GameQueueConfig[] queues = await XmppLobbyService.GetQueues(JsApiService.RiotAccount);
            GameQueueConfig gameQueueConfig = queues.FirstOrDefault<GameQueueConfig>((GameQueueConfig x) => x.Id == (double)num);
            if (gameQueueConfig == null)
            {
                GameQueueConfig[] gameQueueConfigArray = queues;
                gameQueueConfig = (
                    from x in (IEnumerable<GameQueueConfig>)gameQueueConfigArray
                    orderby x.Id
                    select x).FirstOrDefault<GameQueueConfig>();
            }
            GameQueueConfig gameQueueConfig1 = gameQueueConfig;
            if (this.inviteId != str1)
            {
                if (this.role == XmppLobbyService.Role.Host)
                {
                    lock (this.lobbySyncObject)
                    {
                        foreach (XmppLobbyService.LobbyMember member in this.members)
                        {
                            this.XmppMessage(JsApiService.GetSummonerJidFromId(member.SummonerId), "GAME_INVITE_OWNER_CANCEL", string.Empty);
                        }
                    }
                }
                else if (this.respondStateObject != null)
                {
                    string xml = this.respondStateObject.Xml;
                    string summonerJidFromId = JsApiService.GetSummonerJidFromId(this.respondStateObject.HostSummonerId);
                    this.XmppMessage(summonerJidFromId, "GAME_INVITE_REJECT", xml);
                }
                lock (this.lobbySyncObject)
                {
                    this.members.Clear();
                    this.invitations.Clear();
                }
            }
            this.inviteId = str1;
            this.queueId = (int)gameQueueConfig1.Id;
            this.role = role1;
            this.maxMembers = gameQueueConfig1.MaximumParticipantListSize;
            this.autoAcceptInvitation = true;
            this.hostSummonerId = JsApiService.RiotAccount.SummonerId;
            this.hostSummonerName = JsApiService.RiotAccount.SummonerName;
            this.account = JsApiService.RiotAccount;
            this.NotifyAll();
        }

        private async void CreateLobby(int queueId)
        {
            await this.Create(new { queueId = queueId, inviteId = CreateInviteId() });
        }

        private string CreateInviteId()
        {
            Random rnd = new Random();
            return String.Format("INVITE", rnd.Next(9), rnd.Next(9), rnd.Next(9), rnd.Next(9), rnd.Next(9));
            
        }

        private static XmppLobbyService.InvitationPassbackData DecodePassbackString(string str)
        {
            byte[] bytes = str.ToBytes();
            return JsonConvert.DeserializeObject<XmppLobbyService.InvitationPassbackData>(Encoding.UTF8.GetString(bytes));
        }

        private static string EncodePassbackString(XmppLobbyService.InvitationPassbackData data)
        {
            string str = JsonConvert.SerializeObject(data);
            return Encoding.UTF8.GetBytes(str).ToHex();
        }

        private static async Task<GameQueueConfig> GetQueueById(RiotAccount account, int queueId)
        {
            GameQueueConfig[] queues = await XmppLobbyService.GetQueues(account);
            GameQueueConfig gameQueueConfig = queues.FirstOrDefault<GameQueueConfig>((GameQueueConfig x) => x.Id == (double)queueId);
            return gameQueueConfig;
        }

        private static Task<GameQueueConfig[]> GetQueues(RiotAccount account)
        {
            return account.InvokeCachedAsync<GameQueueConfig[]>("matchmakerService", "getAvailableQueues");
        }

        [MicroApiMethod("invite")]
        public async Task Invite(dynamic args)
        {
            if (this.account == null)
            {
                throw new JsApiException("no-account");
            }
            if (this.account.Game != null)
            {
                throw new JsApiException("in-game");
            }
            if (!this.role.HasFlag(XmppLobbyService.Role.Host))
            {
                throw new JsApiException("not-host");
            }
            string summonerName = (string)args.invitorName;
            if (summonerName == null)
            {
                summonerName = this.account.SummonerName;
            }
            string str = summonerName;
            string str1 = (string)args.jid;
            string str2 = (string)args.summonerName;
            if (str1 == null)
            {
                PublicSummoner summoner = await JsApiService.GetSummoner(this.account.RealmId, str2);
                await this.Invite(summoner.SummonerId, str);
            }
            else
            {
                await this.Invite(JsApiService.GetSummonerIdFromJid(str1), str);
            }
        }

        private async Task Invite(long summonerId, string invitorName)
        {
            object obj = null;
            string summonerNameBySummonerId = await JsApiService.GetSummonerNameBySummonerId(this.account.RealmId, summonerId);
            string str = summonerNameBySummonerId;
            GameQueueConfig queueById = await XmppLobbyService.GetQueueById(this.account, this.queueId);
            GameQueueConfig gameQueueConfig = queueById;
            string str1 = (gameQueueConfig.Ranked ? "RANKED_GAME_PREMADE" : "NORMAL_GAME");
            int num = gameQueueConfig.SupportedMapIds.FirstOrDefault<int>();
            object[] xElement = new object[1];
            XName xName = "body";
            object[] objArray = new object[] { new XElement("inviteId", this.inviteId), new XElement("userName", invitorName), new XElement("profileIcon", (object)0), new XElement("gameType", str1), new XElement("mapId", (object)num), new XElement("queueId", (object)this.queueId), new XElement("gameMode", "League of Legends") };
            xElement[0] = new XElement(xName, objArray);
            XDocument xDocument = new XDocument(xElement);
            bool flag = false;
            try
            {
                object obj1 = this.lobbySyncObject;
                object obj2 = obj1;
                obj = obj1;
                Monitor.Enter(obj2, ref flag);
                HashSet<XmppLobbyService.LobbyMember> lobbyMembers = this.invitations;
                XmppLobbyService.LobbyMember lobbyMember = new XmppLobbyService.LobbyMember()
                {
                    SummonerId = summonerId,
                    Name = str
                };
                lobbyMembers.Add(lobbyMember);
            }
            finally
            {
                if (flag)
                {
                    Monitor.Exit(obj);
                }
            }
            this.NotifyInvitations();
            string summonerJidFromId = JsApiService.GetSummonerJidFromId(summonerId);
            this.XmppMessage(summonerJidFromId, "GAME_INVITE", xDocument.ToString(SaveOptions.DisableFormatting));
        }

        private static bool IsSupportedSubject(string subject)
        {
            string str = subject;
            string str1 = str;
            if (str != null)
            {
                switch (str1)
                {
                    case "GAME_INVITE":
                    case "GAME_INVITE_ACCEPT":
                    case "GAME_INVITE_ACCEPT_ACK":
                    case "GAME_INVITE_ALLOW_SUGGESTIONS":
                    case "GAME_INVITE_CANCEL":
                    case "GAME_INVITE_DISALLOW_SUGGESTIONS":
                    case "GAME_INVITE_LIST_STATUS":
                    case "GAME_INVITE_OWNER_CANCEL":
                    case "GAME_INVITE_REJECT":
                    case "GAME_INVITE_REJECT_GAME_FULL":
                    case "GAME_INVITE_SUGGEST":
                    case "GAME_MSG_OUT_OF_SYNC":
                    case "PRACTICE_GAME_INVITE":
                    case "PRACTICE_GAME_INVITE_ACCEPT":
                    case "PRACTICE_GAME_INVITE_ACCEPT_ACK":
                    case "PRACTICE_GAME_JOIN":
                    case "PRACTICE_GAME_JOIN_ACK":
                    case "PRACTICE_GAME_OWNER_CHANGE":
                    case "VERIFY_INVITEE":
                    case "VERIFY_INVITEE_ACK":
                    case "VERIFY_INVITEE_NAK":
                    case "VERIFY_INVITEE_RESET":
                        {
                            return true;
                        }
                }
            }
            return false;
        }

        [MicroApiMethod("leave")]
        public void Leave(dynamic args)
        {
            this.XmppMessage(JsApiService.GetSummonerJidFromId(this.hostSummonerId), "GAME_INVITE_REJECT", null);
            this.role = XmppLobbyService.Role.None;
            this.NotifyRole();
        }

        private void NotifyAll()
        {
            this.NotifyRole();
            this.NotifyQueueId();
            this.NotifyInviteId();
            this.NotifyInvitations();
            this.NotifyMembers();
        }

        private void NotifyInvitations()
        {
            JsApiService.Push("lobby:invitations", this.invitations);
        }

        private void NotifyInviteId()
        {
            JsApiService.Push("lobby:join", new { InviteId = this.inviteId, HostName = this.hostSummonerName });
        }

        private void NotifyMembers()
        {
            XmppLobbyService.LobbyMember[] array;
            XmppLobbyService.LobbyMember[] lobbyMemberArray;
            lock (this.lobbySyncObject)
            {
                RiotAccount riotAccount = JsApiService.RiotAccount;
                if (this.role == XmppLobbyService.Role.Host)
                {
                    XmppLobbyService.LobbyMember[] lobbyMemberArray1 = new XmppLobbyService.LobbyMember[1];
                    XmppLobbyService.LobbyMember lobbyMember = new XmppLobbyService.LobbyMember()
                    {
                        Name = riotAccount.SummonerName,
                        SummonerId = riotAccount.SummonerId
                    };
                    lobbyMemberArray1[0] = lobbyMember;
                    lobbyMemberArray = lobbyMemberArray1;
                }
                else
                {
                    XmppLobbyService.LobbyMember[] lobbyMemberArray2 = new XmppLobbyService.LobbyMember[1];
                    XmppLobbyService.LobbyMember lobbyMember1 = new XmppLobbyService.LobbyMember()
                    {
                        Name = this.hostSummonerName,
                        SummonerId = this.hostSummonerId
                    };
                    lobbyMemberArray2[0] = lobbyMember1;
                    lobbyMemberArray = lobbyMemberArray2;
                }
                XmppLobbyService.LobbyMember[] lobbyMemberArray3 = lobbyMemberArray;
                array = lobbyMemberArray3.Concat<XmppLobbyService.LobbyMember>(this.members).ToArray<XmppLobbyService.LobbyMember>();
            }
            JsApiService.Push("lobby:members", array);
        }

        private void NotifyQueueId()
        {
            JsApiService.Push("lobby:queueId", this.queueId);
        }

        private void NotifyRole()
        {
            JsApiService.Push("lobby:role", this.role);
        }

        private void OnAccountStateChanged(object sender, StateChangedEventArgs args)
        {
            if (args.NewState == ConnectionState.Connected && this.account != sender)
            {
                this.CreateLobby(this.queueId);
            }
        }

        private void OnData(RiotAccount account, object message)
        {
            if (account == this.account)
            {
                GameDTO gameDTO = message as GameDTO;
                GameDTO gameDTO1 = gameDTO;
                if (gameDTO != null)
                {
                    if (gameDTO1.GameState == "START_REQUESTED" || gameDTO1.GameState == "IN_PROGRESS")
                    {
                        this.autoAcceptInvitation = false;
                        if (this.members != null)
                        {
                            this.members.Clear();
                        }
                        this.CreateLobby(this.queueId);
                    }
                    return;
                }
            }
        }

        private void OnFlexMessageReceived(object sender, RtmpSharp.Messaging.MessageReceivedEventArgs e)
        {
            this.OnData(sender as RiotAccount, e.Body);
        }

        private void OnInvocationResult(object sender, InvocationResultEventArgs args)
        {
            this.OnData(sender as RiotAccount, args.Result);
        }

        private void OnMailReceived(object sender, Chat.MessageReceivedEventArgs args)
        {
            this.ProcessMail(sender, args);
        }

        private void OnMessageReceived(object sender, Chat.MessageReceivedEventArgs args)
        {
            try
            {
                this.ProcessMessage(sender, args);
            }
            catch (Exception exception)
            {
            }
        }

        private async Task ProcessMail(object sender, Chat.MessageReceivedEventArgs args)
        {
            string str;
            object obj = null;
            XmppLobbyService.LobbyMember lobbyMember;
            object obj1 = null;
            object obj2 = null;
            if (XmppLobbyService.IsSupportedSubject(args.Subject))
            {
                UberChatClient uberChatClient = (UberChatClient)sender;
                RiotAccount account = uberChatClient.Account;
                string bareJid = args.Sender.BareJid;
                long summonerIdFromJid = JsApiService.GetSummonerIdFromJid(bareJid);
                if (args.Subject == "GAME_INVITE_OWNER_CANCEL")
                {
                    if (summonerIdFromJid == this.hostSummonerId)
                    {
                        JsApiService.Push("lobby:disband", new { hostSummonerName = this.hostSummonerName });
                        this.CreateLobby(this.queueId);
                    }
                }
                else if (args.Subject != "GAME_INVITE_REJECT")
                {
                    XElement xElement = XDocument.Parse(args.Message).Element("body");
                    if (!this.role.HasFlag(XmppLobbyService.Role.Invitee) || summonerIdFromJid != this.hostSummonerId || !(args.Subject == "GAME_INVITE_LIST_STATUS"))
                    {
                        string value = xElement.Element("inviteId").Value;
                        if (args.Subject != "GAME_INVITE")
                        {
                            if (this.role.HasFlag(XmppLobbyService.Role.Host) && this.inviteId == value)
                            {
                                bool flag = false;
                                try
                                {
                                    object obj3 = this.lobbySyncObject;
                                    object obj4 = obj3;
                                    obj = obj3;
                                    Monitor.Enter(obj4, ref flag);
                                    HashSet<XmppLobbyService.LobbyMember> lobbyMembers = this.invitations;
                                    lobbyMember = lobbyMembers.FirstOrDefault<XmppLobbyService.LobbyMember>((XmppLobbyService.LobbyMember x) => x.SummonerId == summonerIdFromJid);
                                }
                                finally
                                {
                                    if (flag)
                                    {
                                        Monitor.Exit(obj);
                                    }
                                }
                                string subject = args.Subject;
                                string str1 = subject;
                                if (subject != null)
                                {
                                    if (str1 == "GAME_INVITE_ACCEPT")
                                    {
                                        bool flag1 = false;
                                        try
                                        {
                                            object obj5 = this.lobbySyncObject;
                                            object obj6 = obj5;
                                            obj1 = obj5;
                                            Monitor.Enter(obj6, ref flag1);
                                            if (lobbyMember == null)
                                            {
                                                str = "GAME_MSG_OUT_OF_SYNC";
                                            }
                                            else if (this.members.Count >= this.maxMembers)
                                            {
                                                str = "GAME_INVITE_REJECT_GAME_FULL";
                                                this.invitations.Remove(lobbyMember);
                                                this.NotifyInvitations();
                                            }
                                            else
                                            {
                                                str = "GAME_INVITE_ACCEPT_ACK";
                                                this.invitations.Remove(lobbyMember);
                                                this.members.Add(lobbyMember);
                                                this.NotifyMembers();
                                                this.NotifyInvitations();
                                            }
                                            this.XmppMessage(bareJid, str, args.Message);
                                        }
                                        finally
                                        {
                                            if (flag1)
                                            {
                                                Monitor.Exit(obj1);
                                            }
                                        }
                                        return;
                                    }
                                    else if (str1 == "GAME_INVITE_SUGGEST")
                                    {
                                        string bareJid1 = args.Sender.BareJid;
                                        bool flag2 = false;
                                        try
                                        {
                                            object obj7 = this.lobbySyncObject;
                                            object obj8 = obj7;
                                            obj2 = obj7;
                                            Monitor.Enter(obj8, ref flag2);
                                            HashSet<XmppLobbyService.LobbyMember> lobbyMembers1 = this.members;
                                            if (lobbyMembers1.All<XmppLobbyService.LobbyMember>((XmppLobbyService.LobbyMember x) => x.SummonerId != summonerIdFromJid))
                                            {
                                                return;
                                            }
                                        }
                                        finally
                                        {
                                            if (flag2)
                                            {
                                                Monitor.Exit(obj2);
                                            }
                                        }
                                        string user = (new JabberId(bareJid1)).User;
                                        XmppLobbyService xmppLobbyService = this;
                                        string value1 = xElement.Element("suggestedInviteJid").Value;
                                        string summonerNameByJid = await JsApiService.GetSummonerNameByJid(account.RealmId, user);
                                        xmppLobbyService.Invite(new { jid = value1, invitorName = summonerNameByJid });
                                    }
                                    else if (str1 == "VERIFY_INVITEE_ACK")
                                    {
                                        return;
                                    }
                                    else if (str1 == "VERIFY_INVITEE_NAK")
                                    {
                                        return;
                                    }
                                }
                            }
                            if (this.role.HasFlag(XmppLobbyService.Role.Invitee) && summonerIdFromJid == this.hostSummonerId && this.inviteId == value)
                            {
                                string subject1 = args.Subject;
                                string str2 = subject1;
                                if (subject1 != null)
                                {
                                    switch (str2)
                                    {
                                        case "GAME_INVITE_ACCEPT_ACK":
                                            {
                                                this.NotifyInviteId();
                                                return;
                                            }
                                        case "GAME_INVITE_ALLOW_SUGGESTIONS":
                                            {
                                                XmppLobbyService xmppLobbyService1 = this;
                                                xmppLobbyService1.role = xmppLobbyService1.role | XmppLobbyService.Role.Invitor;
                                                this.NotifyRole();
                                                return;
                                            }
                                        case "GAME_INVITE_DISALLOW_SUGGESTIONS":
                                            {
                                                XmppLobbyService xmppLobbyService2 = this;
                                                xmppLobbyService2.role = xmppLobbyService2.role & (XmppLobbyService.Role.Host | XmppLobbyService.Role.Invitee);
                                                this.NotifyRole();
                                                return;
                                            }
                                        case "GAME_INVITE_CANCEL":
                                            {
                                                this.role = XmppLobbyService.Role.None;
                                                this.NotifyRole();
                                                return;
                                            }
                                        case "GAME_INVITE_REJECT_GAME_FULL":
                                            {
                                                JsApiService.Push("lobby:full", value);
                                                this.CreateLobby(this.queueId);
                                                return;
                                            }
                                        case "GAME_MSG_OUT_OF_SYNC":
                                            {
                                                JsApiService.Push("lobby:lostSync", value);
                                                this.CreateLobby(this.queueId);
                                                return;
                                            }
                                        case "VERIFY_INVITEE":
                                            {
                                                await JsApiService.RiotAccount.InvokeAsync<object>("matchmakerService", "acceptInviteForMatchmakingGame", value);
                                                XElement xElement1 = new XElement("body", new XElement("inviteId", value));
                                                this.XmppMessage(bareJid, "VERIFY_INVITEE_ACK", xElement1.ToString(SaveOptions.DisableFormatting));
                                                return;
                                            }
                                    }
                                }
                            }
                        }
                        else
                        {
                            string value2 = xElement.Element("userName").Value;
                            int num = int.Parse(xElement.Element("mapId").Value);
                            int num1 = int.Parse(xElement.Element("queueId").Value);
                            GameQueueConfig queueById = await XmppLobbyService.GetQueueById(account, num1);
                            GameService.JsGameMap[] maps = await GameService.GetMaps(account);
                            GameService.JsGameMap jsGameMap = maps.First<GameService.JsGameMap>((GameService.JsGameMap x) => x.Id == num);
                            GameTypeConfigDTO gameTypeConfigDTO = account.GameTypeConfigs.First<GameTypeConfigDTO>((GameTypeConfigDTO x) => x.Id == (double)queueById.GameTypeConfigId);
                            string summonerNameByJid1 = await JsApiService.GetSummonerNameByJid(account.RealmId, args.Sender.BareJid);
                            string str3 = summonerNameByJid1;
                            XmppLobbyService.InvitationPassbackData invitationPassbackDatum = new XmppLobbyService.InvitationPassbackData()
                            {
                                InviteId = value,
                                MapId = num,
                                QueueId = num1,
                                Handle = account.Handle,
                                HostSummonerId = summonerIdFromJid,
                                HostName = str3,
                                Xml = args.Message
                            };
                            XmppLobbyService.InvitationPassbackData invitationPassbackDatum1 = invitationPassbackDatum;
                            if (!this.autoAcceptInvitation || !this.role.HasFlag(XmppLobbyService.Role.Invitee) || this.hostSummonerId != summonerIdFromJid)
                            {
                                JsApiService.Push("lobby:invite", new { InviteId = value, Contact = ContactsNotificationService.JsContact.Create(uberChatClient, args.Sender), Timestamp = args.Timestamp, Sender = str3, Invitor = value2, MapName = jsGameMap.Name, QueueName = queueById.Type, GameTypeConfigName = gameTypeConfigDTO.Name, Handle = account.Handle, PassbackString = XmppLobbyService.EncodePassbackString(invitationPassbackDatum1) });
                            }
                            else
                            {
                                this.Respond(new { accept = true, handle = account.Handle, passbackObject = invitationPassbackDatum1 });
                            }
                        }
                    }
                    else
                    {
                        this.UpdateRoomFromHost(xElement);
                    }
                }
                else
                {
                    lock (this.lobbySyncObject)
                    {
                        HashSet<XmppLobbyService.LobbyMember> lobbyMembers2 = this.members;
                        if (lobbyMembers2.RemoveWhere((XmppLobbyService.LobbyMember x) => x.SummonerId == summonerIdFromJid) > 0)
                        {
                            this.NotifyMembers();
                        }
                    }
                }
            }
        }

        private void ProcessMessage(object sender, Chat.MessageReceivedEventArgs args)
        {
            if (args.MessageId == null || !args.MessageId.StartsWith("hm_"))
            {
                return;
            }
            if (this.role.HasFlag(XmppLobbyService.Role.Invitee) && string.Equals(this.hostSummonerName, args.Sender.Name, StringComparison.OrdinalIgnoreCase))
            {
                XElement xElement = XDocument.Parse(args.Message).Element("body");
                this.UpdateRoomFromHost(xElement);
            }
        }

        [MicroApiMethod("queue")]
        public async Task Queue()
        {
            object obj = null;
            long[] array;
            bool flag = false;
            try
            {
                object obj1 = this.lobbySyncObject;
                object obj2 = obj1;
                obj = obj1;
                Monitor.Enter(obj2, ref flag);
                long[] summonerId = new long[] { JsApiService.RiotAccount.SummonerId };
                HashSet<XmppLobbyService.LobbyMember> lobbyMembers = this.members;
                array = ((IEnumerable<long>)summonerId).Concat<long>(
                    from x in lobbyMembers
                    select x.SummonerId).ToArray<long>();
            }
            finally
            {
                if (flag)
                {
                    Monitor.Exit(obj);
                }
            }
            RiotAccount riotAccount = this.account;
            MatchMakerParams matchMakerParam = new MatchMakerParams()
            {
                BotDifficulty = "MEDIUM",
                InvitationId = this.inviteId,
                QueueIds = new List<int>()
                {
                    this.queueId
                },
                Team = array.ToList<long>()
            };
            await riotAccount.InvokeAsync<SearchingForMatchNotification>("matchmakerService", "attachTeamToQueue", matchMakerParam);
        }

        [MicroApiMethod("reinvite")]
        public void Reinvite()
        {
            if (!this.role.HasFlag(XmppLobbyService.Role.Host))
            {
                return;
            }
            lock (this.lobbySyncObject)
            {
                XmppLobbyService.LobbyMember[] array = this.members.ToArray<XmppLobbyService.LobbyMember>();
                this.members.Clear();
                this.invitations.Clear();
                XmppLobbyService.LobbyMember[] lobbyMemberArray = array;
                for (int i = 0; i < (int)lobbyMemberArray.Length; i++)
                {
                    XmppLobbyService.LobbyMember lobbyMember = lobbyMemberArray[i];
                    this.Invite(lobbyMember.SummonerId, this.account.SummonerName);
                }
            }
        }

        [MicroApiMethod("respond")]
        public void Respond(dynamic args)
        {
            XmppLobbyService.InvitationPassbackData invitationPassbackDatum;
            bool flag = (bool)args.accept;
            if (args.passbackObject == (dynamic)null)
            {
                if (args.passbackString == (dynamic)null)
                {
                    return;
                }
                invitationPassbackDatum = XmppLobbyService.DecodePassbackString((string)args.passbackString);
            }
            else
            {
                invitationPassbackDatum = (XmppLobbyService.InvitationPassbackData)args.passbackObject;
            }
            int handle = invitationPassbackDatum.Handle;
            long hostSummonerId = invitationPassbackDatum.HostSummonerId;
            string hostName = invitationPassbackDatum.HostName;
            string xml = invitationPassbackDatum.Xml;
            string inviteId = invitationPassbackDatum.InviteId;
            int queueId = invitationPassbackDatum.QueueId;
            int mapId = invitationPassbackDatum.MapId;
            if (flag && JsApiService.RiotAccount.Handle != handle)
            {
                throw new JsApiException("inactive-account");
            }
            if (flag)
            {
                this.Create(new { role = XmppLobbyService.Role.Invitee, inviteId = inviteId, mapId = mapId, queueId = queueId });
                this.hostSummonerId = hostSummonerId;
                this.hostSummonerName = hostName;
                this.respondStateObject = invitationPassbackDatum;
            }
            this.XmppMessage(JsApiService.GetSummonerJidFromId(hostSummonerId), (flag ? "GAME_INVITE_ACCEPT" : "GAME_INVITE_REJECT"), xml);
        }

        private void UpdateRoomFromHost(XElement data)
        {
            XElement xElement = data.Element("invitelist");
            if (xElement == null)
            {
                return;
            }
            XElement[] array = xElement.Elements("invitee").ToArray<XElement>();
            var variable = (
                from invite in (IEnumerable<XElement>)array
                let name = invite.Attribute("name").Value
                let status = invite.Attribute("status").Value
                select new { Status = status, Member = new XmppLobbyService.LobbyMember()
                {
                    Name = name
                } }).ToArray();
            lock (this.lobbySyncObject)
            {
                this.invitations.Clear();
                foreach (var variable1 in 
                    from x in (IEnumerable<AnonymousType10<string, XmppLobbyService.LobbyMember>>)variable
                    where x.Status == "PENDING"
                    select x)
                {
                    this.invitations.Add(variable1.Member);
                }
                this.members.Clear();
                foreach (var variable2 in 
                    from x in (IEnumerable<AnonymousType10<string, XmppLobbyService.LobbyMember>>)variable
                    where x.Status == "ACCEPTED"
                    select x)
                {
                    this.members.Add(variable2.Member);
                }
            }
            this.NotifyInvitations();
            this.NotifyMembers();
        }

        private void XmppMessage(string jid, string subject, string message)
        {
            if (this.account != null && this.account.Chat != null)
            {
                this.account.Chat.Chat.Message(jid, subject, message);
            }
        }

        private class InvitationPassbackData
        {
            public string InviteId;

            public int MapId;

            public int QueueId;

            public int Handle;

            public long HostSummonerId;

            public string HostName;

            public string Xml;

            public InvitationPassbackData()
            {
            }
        }

        private class LobbyMember
        {
            public long SummonerId;

            public string Name;

            public LobbyMember()
            {
            }
        }

        private sealed class LobbyMemberComparer : IEqualityComparer<XmppLobbyService.LobbyMember>
        {
            public readonly static IEqualityComparer<XmppLobbyService.LobbyMember> Instance;

            static LobbyMemberComparer()
            {
                XmppLobbyService.LobbyMemberComparer.Instance = new XmppLobbyService.LobbyMemberComparer();
            }

            public LobbyMemberComparer()
            {
            }

            public bool Equals(XmppLobbyService.LobbyMember x, XmppLobbyService.LobbyMember y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }
                if (object.ReferenceEquals(x, null))
                {
                    return false;
                }
                if (object.ReferenceEquals(y, null))
                {
                    return false;
                }
                if (x.GetType() != y.GetType())
                {
                    return false;
                }
                return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(XmppLobbyService.LobbyMember obj)
            {
                if (obj.Name == null)
                {
                    return 0;
                }
                return obj.Name.ToLowerInvariant().GetHashCode();
            }
        }

        [Flags]
        public enum Role
        {
            None = 0,
            Host = 1,
            Invitee = 2,
            Invitor = 4,
            PowerfulInvitee = 6
        }
    }
}