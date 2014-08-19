using Astral.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WintermintData.Account;
using WintermintData.Protocol;

namespace WintermintClient
{
    internal class LittleClient
    {
        private readonly static TimeSpan DefaultCacheTime;

        public AccountDetails Account;

        private readonly static MemoryCache Cache;

        private AstralClient client;

        public bool Connected
        {
            get
            {
                if (this.client == null)
                {
                    return false;
                }
                return this.client.State == AstralClientState.Connected;
            }
        }

        static LittleClient()
        {
            LittleClient.DefaultCacheTime = TimeSpan.FromMinutes(5);
            NameValueCollection nameValueCollection = new NameValueCollection()
            {
                { "CacheMemoryLimitMegabytes", "10" },
                { "PhysicalMemoryLimitPercentage", "10" }
            };
            LittleClient.Cache = new MemoryCache("LittleClientInvokeCache", nameValueCollection);
        }

        public LittleClient()
        {
        }

        public async Task AuthenticateAsync(string email, string password)
        {
            if (!this.Connected)
            {
                this.ReconstructClient();
                await this.client.ConnectAsync();
            }
            await this.NegotiateProtocolAsync();
            AstralClient astralClient = this.client;
            object[] objArray = new object[] { email, password };
            this.Account = await astralClient.InvokeAsync<AccountDetails>("account.login", objArray);
        }

        private static string GetCacheKey<T>(string method, object[] arguments)
        {
            object[] fullName = new object[] { typeof(T).FullName, arguments, method, (int)arguments.Length, LittleClient.JoinArguments(arguments) };
            return string.Format("{0}>{1}/{2}/{3}/{4}", fullName);
        }

        public Task Invoke(string method)
        {
            return this.Invoke<object>(method);
        }

        public Task Invoke(string method, object argument)
        {
            return this.Invoke<object>(method, argument);
        }

        public Task Invoke(string method, object[] arguments)
        {
            return this.Invoke<object>(method, arguments);
        }

        public Task<T> Invoke<T>(string method)
        {
            return this.Invoke<T>(method, new object[0]);
        }

        public Task<T> Invoke<T>(string method, object argument)
        {
            return this.Invoke<T>(method, new object[] { argument });
        }

        public Task<T> Invoke<T>(string method, object[] arguments)
        {
            return this.client.InvokeAsync<T>(method, arguments);
        }

        public Task<T> InvokeCached<T>(string method)
        {
            return this.InvokeCached<T>(method, new object[0]);
        }

        public Task<T> InvokeCached<T>(string method, object argument)
        {
            return this.InvokeCached<T>(method, new object[] { argument });
        }

        public Task<T> InvokeCached<T>(string method, object[] arguments)
        {
            return this.InvokeCached<T>(method, arguments, LittleClient.DefaultCacheTime);
        }

        public Task<T> InvokeCached<T>(string method, TimeSpan life)
        {
            return this.InvokeCached<T>(method, new object[0], life);
        }

        public Task<T> InvokeCached<T>(string method, object argument, TimeSpan life)
        {
            object[] objArray = new object[] { argument };
            return this.InvokeCached<T>(method, objArray, life);
        }

        public Task<T> InvokeCached<T>(string method, object[] arguments, TimeSpan life)
        {
            Task<T> task;
            string cacheKey = LittleClient.GetCacheKey<T>(method, arguments);
            lock (LittleClient.Cache)
            {
                Task<T> task1 = (Task<T>)LittleClient.Cache.Get(cacheKey, null);
                if (task1 == null || task1.IsCompleted && task1.Status != TaskStatus.RanToCompletion)
                {
                    task1 = this.Invoke<T>(method, arguments);
                    LittleClient.Cache.Set(cacheKey, task1, DateTimeOffset.UtcNow + life, null);
                    task = task1;
                }
                else
                {
                    task = task1;
                }
            }
            return task;
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

        private async Task NegotiateProtocolAsync()
        {
            AstralClient astralClient = this.client;
            NegotiationRequest negotiationRequest = new NegotiationRequest()
            {
                Protocols = new long[] { (long)-9999997 }
            };
            NegotiationResponse negotiationResponse = await astralClient.InvokeAsync<NegotiationResponse>("protocol.negotiate", negotiationRequest);
            if (negotiationResponse.ReassignedServer != null)
            {
                throw new NotSupportedException("LittleClient (branch: master) does not support server re-assignment.");
            }
            if (negotiationResponse.Protocol != (long)-9999997)
            {
                throw new ProtocolVersionNegotiationException(negotiationResponse.Protocol > (long)-9999997);
            }
        }

        private void OnDisconnected(object sender, EventArgs args)
        {
            if (this.Disconnected != null)
            {
                this.Disconnected(this, args);
            }
        }

        public void Purge<T>(string method)
        {
            this.Purge<T>(method, new object[0]);
        }

        public void Purge<T>(string method, object argument)
        {
            this.Purge<T>(method, new object[] { argument });
        }

        public void Purge<T>(string method, object[] arguments)
        {
            string cacheKey = LittleClient.GetCacheKey<T>(method, arguments);
            LittleClient.Cache.Remove(cacheKey, null);
        }

        private void ReconstructClient()
        {
            this.client = new AstralClient("ws://1.api.client.wintermint.net:81");
            this.client.Disconnected += new EventHandler(this.OnDisconnected);
        }

        public int Subscribe(string topic, AstralMessageHandler handler)
        {
            return this.client.Subscribe(topic, handler);
        }

        public int Subscribe(Regex topic, AstralMessageHandler handler)
        {
            return this.client.Subscribe(topic, handler);
        }

        public void Unsubscribe(string topic)
        {
            this.client.Unsubscribe(topic);
        }

        public void Unsubscribe(AstralMessageHandler handler)
        {
            this.client.Unsubscribe(handler);
        }

        public void Unsubscribe(int subscriptionId)
        {
            this.client.Unsubscribe(subscriptionId);
        }

        public event EventHandler Disconnected;
    }
}