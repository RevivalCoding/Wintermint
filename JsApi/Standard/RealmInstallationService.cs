using Complete.IO;
using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.Daemons;
using WintermintClient.Data;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Standard.Riot;
using WintermintData.Riot.RealmDownload;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("realm")]
    public class RealmInstallationService : JsApiService
    {
        public RealmInstallationService()
        {
        }

        public string GetRadsDirectory(string realmId)
        {
            return Path.Combine(LaunchData.RiotContainerDirectory, string.Format("league#{0}", realmId), "rads");
        }

        [MicroApiMethod("installationState")]
        public async Task<object> IsPlayable(dynamic args)
        {
            bool flag;
            bool flag1;
            var variable;
            string str = (string)args.realmId;
            RiotUpdateDaemon.UpdaterState updaterState = Instances.RiotUpdater.Updates.FirstOrDefault<RiotUpdateDaemon.UpdaterState>((RiotUpdateDaemon.UpdaterState x) => x.RealmId == str);
            string radsDirectory = this.GetRadsDirectory(str);
            string str1 = Path.Combine(radsDirectory, "lock");
            RealmDownloadsDescription realmDownloadsDescription = await DownloadsService.Get();
            bool flag2 = realmDownloadsDescription.Downloads.Any<RealmDownloadItem>((RealmDownloadItem x) => string.Equals(str, x.RealmId, StringComparison.OrdinalIgnoreCase));
            flag = (!flag2 ? false : Directory.Exists(radsDirectory));
            flag1 = (!flag2 ? false : FileEx.IsFileLocked(str1));
            if (!flag2 || updaterState == null)
            {
                variable = null;
            }
            else
            {
                variable = new { Status = updaterState.Status, Position = updaterState.Position, Length = updaterState.Length };
            }
            object obj = new { Exists = flag, Locked = flag1, Update = variable };
            return obj;
        }
    }
}