using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Notification
{
    [MicroApiSingleton]
    public class AccountBagNotificationService : JsApiService
    {
        public AccountBagNotificationService()
        {
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>(this.OnBagStateChanged);
            JsApiService.AccountBag.AccountRemoved += new EventHandler<RiotAccount>(this.OnBagStateChanged);
            JsApiService.AccountBag.ActiveChanged += new EventHandler<RiotAccount>(this.OnActiveAccountChanged);
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                account.StateChanged += new EventHandler<StateChangedEventArgs>(this.OnAccountStateChanged);
                account.QueuePositionChanged += new EventHandler<int>(this.OnQueuePositionChanged);
                account.WaitDelayChanged += new EventHandler<DateTime>(this.OnWaitDelayChanged);
                JsApiService.Push("wm:account:added", AccountBagNotificationService.TransformAccount(account));
            });
            JsApiService.AccountBag.AccountRemoved += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                account.StateChanged -= new EventHandler<StateChangedEventArgs>(this.OnAccountStateChanged);
                account.QueuePositionChanged -= new EventHandler<int>(this.OnQueuePositionChanged);
                account.WaitDelayChanged -= new EventHandler<DateTime>(this.OnWaitDelayChanged);
                JsApiService.Push("wm:account:removed", AccountBagNotificationService.TransformAccount(account));
            });
        }

        private void NotifyAccounts()
        {
            RiotAccountBag accountBag = JsApiService.AccountBag;
            object[] array = accountBag.GetAll().Select<RiotAccount, object>(new Func<RiotAccount, object>(AccountBagNotificationService.TransformAccount)).OrderBy<object, object>((object x) => x.Username).ToArray<object>();
            JsApiService.Push("wm:accounts", array);
            JsApiService.Push("wm:accounts:active", AccountBagNotificationService.TransformAccount(JsApiService.AccountBag.Active));
        }

        private void OnAccountStateChanged(object sender, StateChangedEventArgs args)
        {
            this.NotifyAccounts();
        }

        private void OnActiveAccountChanged(object sender, RiotAccount riotAccount)
        {
            this.NotifyAccounts();
        }

        private void OnBagStateChanged(object sender, RiotAccount account)
        {
            this.NotifyAccounts();
        }

        private void OnQueuePositionChanged(object sender, int i)
        {
            this.NotifyAccounts();
        }

        private void OnWaitDelayChanged(object sender, DateTime waitingUntil)
        {
            this.NotifyAccounts();
        }

        private static object TransformAccount(RiotAccount account)
        {
            if (account == null)
            {
                return null;
            }
            return new { Username = account.Username, Name = account.SummonerName, AccountId = account.AccountId, SummonerId = account.SummonerId, RealmId = account.RealmId, RealmName = account.RealmName, RealmFullName = account.RealmFullName, Handle = account.Handle, Active = account == JsApiService.AccountBag.Active, State = account.State, QueuePosition = account.QueuePosition, WaitingUntil = account.WaitingUntil, ErrorReason = account.ErrorReason };
        }
    }
}