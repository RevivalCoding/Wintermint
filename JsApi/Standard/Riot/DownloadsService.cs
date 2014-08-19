using Complete.Async;
using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.JsApi;
using WintermintData.Riot.Account;
using WintermintData.Riot.RealmDownload;

namespace WintermintClient.JsApi.Standard.Riot
{
    [MicroApiService("riot.downloads")]
    public class DownloadsService : JsApiService
    {
        private readonly static AsyncLock AsyncLock;

        static DownloadsService()
        {
            DownloadsService.AsyncLock = new AsyncLock();
        }

        public DownloadsService()
        {
        }

        [MicroApiMethod("get")]
        public static async Task<RealmDownloadsDescription> Get()
        {
            RealmDownloadsDescription realmDownloadsDescription;
            AsyncLock.Releaser releaser = await DownloadsService.AsyncLock.LockAsync();
            try
            {
                realmDownloadsDescription = await JsApiService.Client.InvokeCached<RealmDownloadsDescription>("storage.get", "download-realms");
            }
            finally
            {
                ((IDisposable)releaser).Dispose();
            }
            return realmDownloadsDescription;
        }

        [MicroApiMethod("set")]
        public static async Task Set(dynamic args)
        {
            AsyncLock.Releaser releaser = await DownloadsService.AsyncLock.LockAsync();
            try
            {
                JToken jTokens = (JToken)args.downloads;
                RealmDownloadsDescription realmDownloadsDescription = new RealmDownloadsDescription()
                {
                    Downloads = jTokens.ToObject<RealmDownloadItem[]>()
                };
                AccountSettings accountSetting = await JsApiService.Client.Invoke<AccountSettings>("riot.accounts.get", false);
                AccountConfig[] accounts = accountSetting.Accounts;
                realmDownloadsDescription.Downloads = (
                    from x in realmDownloadsDescription.Downloads
                    where accounts.Any<AccountConfig>((AccountConfig account) => string.Equals(account.RealmId, x.RealmId, StringComparison.OrdinalIgnoreCase))
                    select x).ToArray<RealmDownloadItem>();
                LittleClient client = JsApiService.Client;
                object[] objArray = new object[] { "download-realms", realmDownloadsDescription };
                await client.Invoke<object>("storage.set", objArray);
                JsApiService.Client.Purge<RealmDownloadsDescription>("storage.get", "download-realms");
            }
            finally
            {
                ((IDisposable)releaser).Dispose();
            }
        }
    }
}