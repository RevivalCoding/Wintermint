using MicroApi;
using System;
using System.Runtime.CompilerServices;
using WintermintClient;
using WintermintClient.Daemons;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Notification
{
    [MicroApiSingleton]
    public class RiotUpdateService : JsApiService
    {
        public RiotUpdateService()
        {
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>((object sender, RiotAccount account) => account.StateChanged += new EventHandler<StateChangedEventArgs>(this.AccountOnStateChanged));
            JsApiService.AccountBag.AccountRemoved += new EventHandler<RiotAccount>((object sender, RiotAccount account) => account.StateChanged -= new EventHandler<StateChangedEventArgs>(this.AccountOnStateChanged));
        }

        private void AccountOnStateChanged(object sender, StateChangedEventArgs args)
        {
            RiotAccount riotAccount = (RiotAccount)sender;
            if (args.NewState == ConnectionState.Connected)
            {
                RiotUpdateDaemon riotUpdater = Instances.RiotUpdater;
                string[] realmId = new string[] { riotAccount.RealmId };
                riotUpdater.TryUpdate(realmId);
            }
        }
    }
}