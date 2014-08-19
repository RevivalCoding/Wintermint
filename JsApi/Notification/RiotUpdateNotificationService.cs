using MicroApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.Daemons;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Standard.Riot;
using WintermintData.Riot;

namespace WintermintClient.JsApi.Notification
{
    [MicroApiSingleton]
    public class RiotUpdateNotificationService : JsApiService
    {
        public RiotUpdateNotificationService()
        {
            RiotUpdateDaemon riotUpdater = Instances.RiotUpdater;
            riotUpdater.Changed += new EventHandler<RiotUpdateDaemon.UpdaterState[]>((object sender, RiotUpdateDaemon.UpdaterState[] states) => this.OnUpdateProgressAsync(states));
            riotUpdater.Completed += new EventHandler<RiotUpdateDaemon.UpdaterState>((object sender, RiotUpdateDaemon.UpdaterState state) => this.UpdaterOnCompletedAsync(state));
        }

        private static object GetJsonDto(SupportedRealm[] realms, RiotUpdateDaemon.UpdaterState update)
        {
            Func<string, object> func = (string realmId) => realms.FirstOrDefault<SupportedRealm>((SupportedRealm x) => realmId.Equals(x.Id, StringComparison.OrdinalIgnoreCase));
            return new { realm = func(update.RealmId), status = update.Status, position = update.Position, length = update.Length };
        }

        private async Task OnUpdateProgressAsync(RiotUpdateDaemon.UpdaterState[] updates)
        {
            SupportedRealm[] realms = await RealmService.GetRealms();
            RiotUpdateDaemon.UpdaterState[] updaterStateArray = updates;
            IEnumerable<object> completed =
                from update in (IEnumerable<RiotUpdateDaemon.UpdaterState>)updaterStateArray
                where !update.Completed
                select RiotUpdateNotificationService.GetJsonDto(realms, update);
            JsApiService.Push("update:riot:progress", completed);
        }

        private async Task UpdaterOnCompletedAsync(RiotUpdateDaemon.UpdaterState update)
        {
            SupportedRealm[] realms = await RealmService.GetRealms();
            JsApiService.Push("update:riot:completed", RiotUpdateNotificationService.GetJsonDto(realms, update));
        }
    }
}