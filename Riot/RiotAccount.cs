using Chat;
using Complete.Async;
using RiotGames.Platform.Clientfacade.Domain;
using RiotGames.Platform.Game;
using RiotGames.Platform.Gameclient.Domain;
using RiotGames.Platform.Messaging;
using RiotGames.Platform.Summoner;
using RiotGames.Platform.Systemstate;
using RiotSharp;
using RtmpSharp.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.Data;
using WintermintData.Riot.Account;

namespace WintermintClient.Riot
{
    public class RiotAccount
    {
        private const int kDefaultCacheLifeSeconds = 300;

        private static int handleCounter;

        private readonly static MemoryCache Cache;

        public readonly int Handle = Interlocked.Increment(ref RiotAccount.handleCounter);

        public bool CanConnect;

        public GameDTO Game_;

        public string PlatformId;

        private volatile ConnectionState state;

        private int pendingInvocations;

        private readonly UberChatClient chat;

        private readonly RiotRtmpClient rtmp;

        private readonly RollingList<DateTime> reconnectAttempts;

        private readonly AsyncLock reconnectLock = new AsyncLock();

        private int pendingReconnects;

        private static int debugFlushCounter;

        public long AccountId
        {
            get;
            private set;
        }

        public Dictionary<string, Func<string>> Blockers
        {
            get;
            private set;
        }

        public static TimeSpan CachedTtl
        {
            get;
            set;
        }

        public UberChatClient Chat
        {
            get
            {
                return this.chat;
            }
        }

        public Endpoints Endpoints
        {
            get;
            private set;
        }

        public string ErrorReason
        {
            get;
            private set;
        }

        public GameDTO Game
        {
            get
            {
                return this.Game_;
            }
            set
            {
                if (this.Game_ != null)
                {
                    this.LastNonNullGame = this.Game_;
                }
                if (this.LastNonNullGame == null)
                {
                    this.LastNonNullGame = value;
                }
                this.Game_ = value;
            }
        }

        public List<GameTypeConfigDTO> GameTypeConfigs
        {
            get;
            private set;
        }

        public bool IsBlocked
        {
            get
            {
                if (this.state != ConnectionState.Connected)
                {
                    return false;
                }
                return !(
                    from x in this.Blockers.Values
                    select x()).All<string>(new Func<string, bool>(string.IsNullOrEmpty));
            }
        }

        public GameDTO LastNonNullGame
        {
            get;
            private set;
        }

        public string Password
        {
            get;
            private set;
        }

        public int PendingInvocations
        {
            get
            {
                return this.pendingInvocations;
            }
        }

        public List<GameTypeConfigDTO> PracticeGameTypeConfigs
        {
            get;
            private set;
        }

        public int QueuePosition
        {
            get;
            private set;
        }

        public string RealmFullName
        {
            get;
            private set;
        }

        public string RealmId
        {
            get;
            private set;
        }

        public string RealmName
        {
            get;
            private set;
        }

        public ConnectionState State
        {
            get
            {
                return (ConnectionState)this.state;
            }
        }

        public Dictionary<string, object> Storage
        {
            get;
            private set;
        }

        public long SummonerId
        {
            get;
            private set;
        }

        public string SummonerInternalName
        {
            get;
            private set;
        }

        public string SummonerName
        {
            get;
            private set;
        }

        public string Username
        {
            get;
            private set;
        }

        public DateTime WaitingUntil
        {
            get;
            private set;
        }

        static RiotAccount()
        {
            RiotAccount.CachedTtl = TimeSpan.FromSeconds(300);
            NameValueCollection nameValueCollection = new NameValueCollection()
            {
                { "CacheMemoryLimitMegabytes", "10" },
                { "PhysicalMemoryLimitPercentage", "10" }
            };
            RiotAccount.Cache = new MemoryCache("riot-account-invocation-cache", nameValueCollection);
        }

        public RiotAccount(AccountConfig config)
        {
            this.CanConnect = true;
            this.Blockers = new Dictionary<string, Func<string>>();
            this.Storage = new Dictionary<string, object>();
            this.RealmId = config.RealmId;
            this.RealmName = config.RealmName;
            this.RealmFullName = config.RealmFullName;
            this.Username = config.Username;
            this.Password = config.Password;
            this.Endpoints = config.Endpoints;
            this.rtmp = new RiotRtmpClient(RtmpSharpData.SerializationContext, config.Endpoints.Rtmp.Uri, config.Username, config.Password, config.Endpoints.Rtmp.AuthDomain, config.Endpoints.Rtmp.ServiceEndpoint, config.Endpoints.Rtmp.Versions, config.Endpoints.Rtmp.LoginQueueUri);
            this.rtmp.Disconnected += new EventHandler((object sender, EventArgs args) => this.OnDisconnected(args));
            this.rtmp.LoginQueuePositionChanged += new EventHandler<int>((object sender, int i) => this.OnLoginQueuePositionChanged(i));
            this.rtmp.MessageReceived += new EventHandler<RtmpSharp.Messaging.MessageReceivedEventArgs>((object sender, RtmpSharp.Messaging.MessageReceivedEventArgs args) => this.OnMessageReceived(args));
            this.chat = ChatHost.Create(config, this);
            this.chat.Disconnected += new EventHandler(this.OnChatDisconnected);
            this.reconnectAttempts = new RollingList<DateTime>(15);
        }

        public void Close()
        {
            try
            {
                this.chat.Close();
            }
            catch
            {
            }
            try
            {
                this.rtmp.Close();
            }
            catch
            {
            }
        }

        public async Task ConnectAsync()
        {
            ConnectionState connectionState;
            RiotAccount riotAccount = null;
            bool flag = false;
            try
            {
                RiotAccount riotAccount1 = this;
                RiotAccount riotAccount2 = riotAccount1;
                riotAccount = riotAccount1;
                Monitor.Enter(riotAccount2, ref flag);
                if (this.pendingReconnects <= 1)
                {
                    RiotAccount riotAccount3 = this;
                    riotAccount3.pendingReconnects = riotAccount3.pendingReconnects + 1;
                }
                else
                {
                    return;
                }
            }
            finally
            {
                if (flag)
                {
                    Monitor.Exit(riotAccount);
                }
            }
            try
            {
                using (Task<AsyncLock.Releaser> task = this.reconnectLock.LockAsync())
                {
                    if (this.CanConnect)
                    {
                        try
                        {
                            if (this.state == ConnectionState.Disconnected || this.state == ConnectionState.Error || this.state == ConnectionState.Waiting)
                            {
                                this.UpdateState(ConnectionState.Connecting);
                                this.rtmp.Reset();
                                await this.rtmp.Connect();
                                this.chat.Connect();
                                LoginDataPacket loginDataPacket = await this.rtmp.Invoke<LoginDataPacket>("clientFacadeService", "getLoginDataPacketForUser");
                                if (loginDataPacket.AllSummonerData != null && loginDataPacket.AllSummonerData.Summoner != null)
                                {
                                    Summoner summoner = loginDataPacket.AllSummonerData.Summoner;
                                    this.SummonerName = summoner.Name;
                                    this.SummonerInternalName = summoner.InternalName;
                                    this.SummonerId = (long)summoner.SumId;
                                    this.AccountId = summoner.AccountId;
                                }
                                this.GameTypeConfigs = loginDataPacket.GameTypeConfigs;
                                RiotAccount list = this;
                                int[] practiceGameTypeConfigIdList = loginDataPacket.ClientSystemStates.PracticeGameTypeConfigIdList;
                                IEnumerable<GameTypeConfigDTO> gameTypeConfigDTOs =
                                    from id in (IEnumerable<int>)practiceGameTypeConfigIdList
                                    select this.GameTypeConfigs.FirstOrDefault<GameTypeConfigDTO>((GameTypeConfigDTO config) => config.Id == (double)id);
                                list.PracticeGameTypeConfigs = (
                                    from x in gameTypeConfigDTOs
                                    where x != null
                                    select x).ToList<GameTypeConfigDTO>();
                                this.UpdateState(ConnectionState.Connected);
                                this.OnConnected();
                            }
                            else
                            {
                                return;
                            }
                        }
                        catch (LoginException loginException1)
                        {
                            LoginException loginException = loginException1;
                            this.ErrorReason = loginException.Reason;
                            RiotAccount riotAccount4 = this;
                            connectionState = (loginException.Type == LoginException.ResponseType.Failed ? ConnectionState.Error : ConnectionState.Disconnected);
                            riotAccount4.UpdateState(connectionState);
                        }
                        catch (Exception exception)
                        {
                            this.UpdateState(ConnectionState.Disconnected);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch
            {
                lock (this)
                {
                    RiotAccount riotAccount5 = this;
                    riotAccount5.pendingReconnects = riotAccount5.pendingReconnects - 1;
                }
            }
        }

        private string GetCacheKey<T>(string destination, string method, object[] arguments)
        {
            string realmId = this.RealmId;
            object[] fullName = new object[] { typeof(T).FullName, realmId, destination, method, (int)arguments.Length, RiotAccount.JoinArguments(arguments) };
            return string.Format("{0}>{1}/{2}/{3}/{4}/{5}", fullName);
        }

        private TimeSpan GetReconnectDelay()
        {
            Func<TimeSpan, int> func = (TimeSpan period) => this.reconnectAttempts.Count((DateTime x) => (DateTime.UtcNow - x) <= period);
            if (func(TimeSpan.FromMinutes(10)) > 10)
            {
                return TimeSpan.FromSeconds(30);
            }
            if (func(TimeSpan.FromMinutes(2)) <= 0)
            {
                return TimeSpan.Zero;
            }
            return TimeSpan.FromSeconds(10);
        }

        public Task<T> InvokeAsync<T>(string destination, string method)
        {
            return this.InvokeAsync<T>(destination, method, new object[0]);
        }

        public Task<T> InvokeAsync<T>(string destination, string method, object argument)
        {
            object[] objArray = new object[] { argument };
            return this.InvokeAsync<T>(destination, method, objArray);
        }

        public async Task<T> InvokeAsync<T>(string destination, string method, object[] arguments)
        {
            RiotAccount.<InvokeAsync<T>> variable = new RiotAccount.<InvokeAsync<T>>();
            variable.this = this;
            variable.destination = destination;
            variable.method = method;
            variable.arguments = arguments;
            variable.builder = AsyncTaskMethodBuilder<T>.Create();
            variable.state = -1;
            variable.builder.Start<RiotAccount.<InvokeAsync<T>>>(ref variable);
            return variable.builder.Task;
        }

        public Task<T> InvokeCachedAsync<T>(string destination, string method, TimeSpan life)
        {
            return this.InvokeCachedAsync<T>(destination, method, new object[0], life);
        }

        public Task<T> InvokeCachedAsync<T>(string destination, string method, object argument, TimeSpan life)
        {
            object[] objArray = new object[] { argument };
            return this.InvokeCachedAsync<T>(destination, method, objArray, life);
        }

        public Task<T> InvokeCachedAsync<T>(string destination, string method, object[] arguments, TimeSpan life)
        {
            Task<T> task;
            string realmId = this.RealmId;
            string cacheKey = this.GetCacheKey<T>(destination, method, arguments);
            lock (RiotAccount.Cache)
            {
                Task<T> task1 = (Task<T>)RiotAccount.Cache.Get(cacheKey, null);
                if (task1 == null || task1.IsCompleted && task1.Status != TaskStatus.RanToCompletion)
                {
                    task1 = this.InvokeAsync<T>(destination, method, arguments);
                    RiotAccount.Cache.Set(cacheKey, task1, DateTimeOffset.UtcNow + life, null);
                    task = task1;
                }
                else
                {
                    task = task1;
                }
            }
            return task;
        }

        public Task<T> InvokeCachedAsync<T>(string destination, string method)
        {
            return this.InvokeCachedAsync<T>(destination, method, new object[0]);
        }

        public Task<T> InvokeCachedAsync<T>(string destination, string method, object argument)
        {
            object[] objArray = new object[] { argument };
            return this.InvokeCachedAsync<T>(destination, method, objArray);
        }

        public Task<T> InvokeCachedAsync<T>(string destination, string method, object[] arguments)
        {
            return this.InvokeCachedAsync<T>(destination, method, arguments, TimeSpan.FromSeconds(300));
        }

        private static string JoinArguments(object[] arguments)
        {
            return string.Join("`", ((IEnumerable<object>)arguments).Select<object, string>((object x) =>
            {
                if (!(x is Array) && !(x is IList))
                {
                    return x.ToString();
                }
                return string.Format("[{0}]", string.Join("!", ((IEnumerable)x).Cast<object>().ToArray<object>()));
            }));
        }

        private async void OnChatDisconnected(object sender, EventArgs args)
        {
            if (this.CanConnect)
            {
                if (string.IsNullOrEmpty(this.ErrorReason))
                {
                    await Task.Delay(1000);
                    this.chat.Connect();
                }
            }
        }

        private void OnConnected()
        {
            this.UpdateState(ConnectionState.Connected);
            if (this.Connected != null)
            {
                this.Connected(this, new EventArgs());
            }
        }

        private void OnDisconnected(EventArgs e)
        {
            this.UpdateState(ConnectionState.Disconnected);
            if (this.Disconnected != null)
            {
                this.Disconnected(this, e);
            }
        }

        private void OnInvocationResult(string service, string method, bool success, object result)
        {
            if (this.InvocationResult != null)
            {
                this.InvocationResult(this, new InvocationResultEventArgs(service, method, success, result));
            }
        }

        private void OnLoginQueuePositionChanged(int position)
        {
            this.QueuePosition = position;
            if (this.QueuePositionChanged != null)
            {
                this.QueuePositionChanged(this, position);
            }
        }

        private void OnMessageReceived(RtmpSharp.Messaging.MessageReceivedEventArgs args)
        {
            if (this.MessageReceived != null)
            {
                this.MessageReceived(this, args);
            }
            if (args.Body is ClientLoginKickNotification)
            {
                this.ErrorReason = "login_invalidated";
                this.UpdateState(ConnectionState.Error);
                this.Close();
                this.ErrorReason = "login_invalidated";
                this.UpdateState(ConnectionState.Error);
            }
        }

        private void OnStateChanged(ConnectionState oldState, ConnectionState newState)
        {
            if (this.StateChanged == null || oldState == newState)
            {
                return;
            }
            this.StateChanged(this, new StateChangedEventArgs(oldState, newState));
        }

        private void OnWaitDelayChanged(DateTime waitingUntil)
        {
            this.WaitingUntil = waitingUntil;
            if (this.WaitDelayChanged != null)
            {
                this.WaitDelayChanged(this, waitingUntil);
            }
        }

        public Task ReconnectAsync()
        {
            return this.ConnectAsync();
        }

        public async Task ReconnectThrottledAsync()
        {
            this.UpdateState(ConnectionState.Waiting);
            TimeSpan reconnectDelay = this.GetReconnectDelay();
            this.OnWaitDelayChanged(DateTime.UtcNow + reconnectDelay);
            await Task.Delay(reconnectDelay);
            this.reconnectAttempts.Push(DateTime.UtcNow);
            await this.ReconnectAsync();
        }

        public void RemoveCached<T>(string destination, string method)
        {
            this.RemoveCached<T>(destination, method, new object[0]);
        }

        public void RemoveCached<T>(string destination, string method, object argument)
        {
            this.RemoveCached<T>(destination, method, new object[] { argument });
        }

        public void RemoveCached<T>(string destination, string method, object[] arguments)
        {
            string cacheKey = this.GetCacheKey<T>(destination, method, arguments);
            RiotAccount.Cache.Remove(cacheKey, null);
        }

        private void UpdateState(ConnectionState state)
        {
            ConnectionState connectionState = this.state;
            if (connectionState == ConnectionState.Error && state == ConnectionState.Disconnected)
            {
                return;
            }
            this.state = state;
            this.OnStateChanged(connectionState, state);
        }

        public event EventHandler Connected;

        public event EventHandler Disconnected;

        public event EventHandler<InvocationResultEventArgs> InvocationResult;

        public event EventHandler<RtmpSharp.Messaging.MessageReceivedEventArgs> MessageReceived;

        public event EventHandler<int> QueuePositionChanged;

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public event EventHandler<DateTime> WaitDelayChanged;
    }
}