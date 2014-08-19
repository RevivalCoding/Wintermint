using Complete.Async;
using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.Daemons;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Helpers;
using WintermintClient.Riot;
using WintermintData.Riot.Account;

namespace WintermintClient.JsApi.Standard.Riot
{
    [MicroApiService("riot.accounts")]
    public class AccountService : JsApiService
    {
        private readonly AsyncLock asyncLock = new AsyncLock();

        public AccountService()
        {
        }

        [MicroApiMethod("activate")]
        public void Activate(dynamic args)
        {
            if (JsApiService.RiotAccount != null && JsApiService.RiotAccount.IsBlocked)
            {
                throw new JsApiException("blocked");
            }
            int num = (int)args.handle;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(num);
            LittleClient client = JsApiService.Client;
            AccountReference accountReference = new AccountReference()
            {
                RealmId = riotAccount.RealmId,
                Username = riotAccount.Username
            };
            client.Invoke("riot.accounts.activate", accountReference);
            JsApiService.AccountBag.SetActive(riotAccount);
        }

        [MicroApiMethod("pull")]
        public async Task DownloadAccounts(JToken args)
        {
            bool flag = false;
            bool flag1;
            flag1 = (!args.Contains<JToken>("cleanse") ? false : args["cleanse"].ToObject<bool>());
            bool flag2 = flag1;
            Func<string, string, bool> func = (string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
            Func<RiotAccount, AccountConfig, bool> username = (RiotAccount config, AccountConfig account) =>
            {
                if (!func(config.Username, account.Username) || !func(config.RealmId, account.RealmId))
                {
                    return false;
                }
                return func(config.Password, account.Password);
            };
            AccountSettings accountSettings = await this.GetAccountSettings();
            AccountConfig[] accounts = accountSettings.Accounts;
            RiotAccount[] all = JsApiService.AccountBag.GetAll();
            if (!flag2)
            {
                RiotAccount[] riotAccountArray = all;
                IEnumerable<RiotAccount> riotAccounts = ((IEnumerable<RiotAccount>)riotAccountArray).Where<RiotAccount>((RiotAccount x) =>
                {
                    if (!accounts.Any<AccountConfig>((AccountConfig config) => username(x, config)))
                    {
                        return true;
                    }
                    return x.State == ConnectionState.Error;
                });
                foreach (RiotAccount riotAccount in riotAccounts)
                {
                    JsApiService.AccountBag.Detach(riotAccount);
                }
            }
            else
            {
                RiotAccount[] riotAccountArray1 = all;
                for (int i = 0; i < (int)riotAccountArray1.Length; i++)
                {
                    RiotAccount riotAccount1 = riotAccountArray1[i];
                    JsApiService.AccountBag.Detach(riotAccount1);
                }
                if (flag)
                {
                }
            }
            AccountConfig[] accountConfigArray = accounts;
            for (int j = 0; j < (int)accountConfigArray.Length; j++)
            {
                AccountConfig accountConfig = accountConfigArray[j];
                JsApiService.AccountBag.Attach(accountConfig);
            }
            RiotAccount riotAccount2 = null;
            RiotAccount[] all1 = JsApiService.AccountBag.GetAll();
            AccountReference active = accountSettings.Active;
            if (active != null)
            {
                RiotAccount[] riotAccountArray2 = all1;
                riotAccount2 = ((IEnumerable<RiotAccount>)riotAccountArray2).FirstOrDefault<RiotAccount>((RiotAccount x) =>
                {
                    if (!string.Equals(active.Username, x.Username, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    return string.Equals(active.RealmId, x.RealmId, StringComparison.OrdinalIgnoreCase);
                });
            }
            if (riotAccount2 == null)
            {
                riotAccount2 = all1.FirstOrDefault<RiotAccount>();
                if (riotAccount2 != null)
                {
                    LittleClient client = JsApiService.Client;
                    AccountReference accountReference = new AccountReference()
                    {
                        Username = riotAccount2.Username,
                        RealmId = riotAccount2.RealmId
                    };
                    client.Invoke("riot.accounts.activate", accountReference);
                }
            }
            JsApiService.AccountBag.SetActive(riotAccount2);
        }

        [MicroApiMethod("list")]
        public async Task<object> GetAccountList()
        {
            JsApiService.AccountBag.GetAll();
            Func<string, string, string> func = (string realmId, string username) =>
            {
                RiotAccount riotAccount = this.accounts.FirstOrDefault<RiotAccount>((RiotAccount x) =>
                {
                    if (!string.Equals(realmId, x.RealmId, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    return string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase);
                });
                if (riotAccount == null)
                {
                    return string.Empty;
                }
                return riotAccount.SummonerName;
            };
            AccountConfig[] accounts = await this.GetAccountSettings().Accounts;
            object obj =
                from account in (IEnumerable<AccountConfig>)accounts
                orderby account.Username
                select new { SummonerName = func(account.RealmId, account.Username), Username = account.Username, Password = account.Password, RealmId = account.RealmId };
            return obj;
        }

        private async Task<AccountSettings> GetAccountSettings()
        {
            AccountSettings accountSetting;
            AsyncLock.Releaser releaser = await this.asyncLock.LockAsync();
            try
            {
                accountSetting = await JsApiService.Client.Invoke<AccountSettings>("riot.accounts.get", true);
            }
            finally
            {
                ((IDisposable)releaser).Dispose();
            }
            return accountSetting;
        }

        [MicroApiMethod("sync")]
        public async Task SyncAccounts(dynamic args)
        {
            AccountService.<SyncAccounts>d__14 variable = new AccountService.<SyncAccounts>d__14();
            variable.<>4__this = this;
            variable.args = args;
            variable.<>t__builder = AsyncTaskMethodBuilder.Create();
            variable.<>1__state = -1;
            variable.<>t__builder.Start<AccountService.<SyncAccounts>d__14>(ref variable);
            return variable.<>t__builder.Task;
        }

        [MicroApiMethod("push")]
        public async Task UploadAccounts(dynamic args)
        {
            AsyncLock.Releaser releaser = await this.asyncLock.LockAsync();
            try
            {
                JArray jArrays = (JArray)args.accounts;
                AccountDto[] array = (
                    from x in jArrays
                    select x.ToObject<AccountDto>()).Distinct<AccountDto>(AccountDtoComparer.Instance).ToArray<AccountDto>();
                LittleClient client = JsApiService.Client;
                await client.Invoke("riot.accounts.set", new object[] { array });
            }
            finally
            {
                ((IDisposable)releaser).Dispose();
            }
        }
    }
}