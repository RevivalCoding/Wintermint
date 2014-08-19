using Complete.Interop.OS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.Data;
using WintermintClient.Riot;
using WintermintData.Riot.RealmDownload;

namespace WintermintClient.Daemons
{
    internal class RiotUpdateDaemon
    {
        private const string ExecutableName = "riot-update";

        private readonly static TimeSpan UpdateCooldownInterval;

        private static TimeSpan InitialUpdateDelay;

        private static TimeSpan UpdateDelay;

        private static Regex RiotUpdaterProgressParser;

        private readonly ConcurrentDictionary<string, RiotUpdateDaemon.UpdaterState> updates;

        private readonly Dictionary<string, DateTime> lastUpdates;

        public RiotUpdateDaemon.UpdaterState[] Updates
        {
            get
            {
                return (
                    from x in this.updates.Values
                    where !x.Completed
                    select x).ToArray<RiotUpdateDaemon.UpdaterState>();
            }
        }

        static RiotUpdateDaemon()
        {
            RiotUpdateDaemon.UpdateCooldownInterval = TimeSpan.FromMinutes(5);
            RiotUpdateDaemon.InitialUpdateDelay = TimeSpan.FromMinutes(5);
            RiotUpdateDaemon.UpdateDelay = TimeSpan.FromHours(1);
            RiotUpdateDaemon.RiotUpdaterProgressParser = new Regex("^(?<status>.*?)\\|(?<position>.*?)\\|(?<length>.*?)$", RegexOptions.Compiled | RegexOptions.ECMAScript);
        }

        public RiotUpdateDaemon()
        {
            this.updates = new ConcurrentDictionary<string, RiotUpdateDaemon.UpdaterState>(StringComparer.OrdinalIgnoreCase);
            this.lastUpdates = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        }

        private bool CanUpdate(string realmId)
        {
            DateTime dateTime;
            bool flag;
            lock (this.lastUpdates)
            {
                flag = (!this.lastUpdates.TryGetValue(realmId, out dateTime) ? true : (DateTime.UtcNow - dateTime) > RiotUpdateDaemon.UpdateCooldownInterval);
            }
            return flag;
        }

        private void DoLocalUpdate(string realmId, string[] arguments)
        {
            RiotUpdateDaemon.UpdaterState updaterState;
            if (!this.CanUpdate(realmId))
            {
                return;
            }
            lock (this.lastUpdates)
            {
                if (!this.updates.TryGetValue(realmId, out updaterState) || updaterState.Completed)
                {
                    this.updates[realmId] = RiotUpdateDaemon.UpdaterState.Create(realmId, arguments, new Action(this.OnChanged), new Action<RiotUpdateDaemon.UpdaterState>(this.OnCompleted));
                    this.lastUpdates[realmId] = DateTime.UtcNow;
                }
            }
        }

        private string GetGameInstallationDirectory(string realmId)
        {
            return string.Format("league#{0}", realmId);
        }

        public void Initialize()
        {
            this.RunLoop();
        }

        private void OnChanged()
        {
            if (this.Changed != null)
            {
                this.Changed(this, this.Updates);
            }
        }

        private void OnCompleted(RiotUpdateDaemon.UpdaterState completedState)
        {
            if (this.Completed != null)
            {
                this.Completed(this, completedState);
            }
        }

        private void RemoveUnusedInstallations(RealmDownloadConfig[] configs)
        {
            string[] array = (
                from x in configs
                select this.GetGameInstallationDirectory(x.RealmId)).ToArray<string>();
            DirectoryInfo[] directories = (new DirectoryInfo(LaunchData.RiotContainerDirectory)).GetDirectories("league#*");
            for (int i = 0; i < (int)directories.Length; i++)
            {
                DirectoryInfo directoryInfo = directories[i];
                string name = directoryInfo.Name;
                if (!array.Any<string>((string x) => name.Equals(x, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        directoryInfo.Delete(true);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private async Task RunLoop()
        {
            RiotUpdateDaemon.<RunLoop>d__1f variable = new RiotUpdateDaemon.<RunLoop>d__1f();
            variable.<>4__this = this;
            variable.<>t__builder = AsyncTaskMethodBuilder.Create();
            variable.<>1__state = -1;
            variable.<>t__builder.Start<RiotUpdateDaemon.<RunLoop>d__1f>(ref variable);
            return variable.<>t__builder.Task;
        }

        public Task TryUpdate()
        {
            string[] array = (
                from x in (IEnumerable<RiotAccount>)Instances.AccountBag.GetAll()
                select x.RealmId).Distinct<string>().ToArray<string>();
            return this.TryUpdate(array);
        }

        public async Task TryUpdate(string[] realmIdSubset)
        {
            string[] array = realmIdSubset.Where<string>(new Func<string, bool>(this.CanUpdate)).ToArray<string>();
            if ((int)array.Length != 0)
            {
                try
                {
                    LittleClient client = Instances.Client;
                    object[] objArray = new object[] { "~> 0.0.0", null };
                    string[] strArrays = new string[] { "windows", "legacy-v0", "pando-interop-disabled", "p2p-disabled" };
                    objArray[1] = strArrays;
                    RealmDownloadConfig[] realmDownloadConfigArray = await client.Invoke<RealmDownloadConfig[]>("riot.downloads.getPatchConfig", objArray);
                    RealmDownloadConfig[] realmDownloadConfigArray1 = realmDownloadConfigArray;
                    RealmDownloadConfig[] array1 = (
                        from x in (IEnumerable<RealmDownloadConfig>)realmDownloadConfigArray1
                        where array.Contains<string>(x.RealmId, StringComparer.OrdinalIgnoreCase)
                        select x).ToArray<RealmDownloadConfig>();
                    if ((int)array1.Length != 0)
                    {
                        this.UpdateInstallations(realmDownloadConfigArray);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception exception)
                {
                }
            }
        }

        private void UpdateInstallations(RealmDownloadConfig[] configs)
        {
            RiotAccount[] all = Instances.AccountBag.GetAll();
            IEnumerable<string> strs = (
                from account in (IEnumerable<RiotAccount>)all
                where !account.IsBlocked
                select account.RealmId).Distinct<string>().Where<string>(new Func<string, bool>(this.CanUpdate));
            RealmDownloadConfig[] array = (
                from x in configs
                where strs.Any<string>((string y) => y.Equals(x.RealmId, StringComparison.OrdinalIgnoreCase))
                select x).ToArray<RealmDownloadConfig>();
            RealmDownloadConfig[] realmDownloadConfigArray = array;
            for (int i = 0; i < (int)realmDownloadConfigArray.Length; i++)
            {
                RealmDownloadConfig realmDownloadConfig = realmDownloadConfigArray[i];
                string str = Path.Combine(LaunchData.RiotContainerDirectory, this.GetGameInstallationDirectory(realmDownloadConfig.RealmId));
                string str1 = Path.Combine(str, "rads");
                string temporaryFolder = LaunchData.GetTemporaryFolder("league-update");
                RealmDownloadParameter[] realmDownloadParameter = new RealmDownloadParameter[] { new RealmDownloadParameter("rads-directory", str1), new RealmDownloadParameter("temporary-directory", temporaryFolder) };
                RealmDownloadParameter[] realmDownloadParameterArray = realmDownloadParameter;
                RealmDownloadParameter[] array1 = realmDownloadParameterArray.Concat<RealmDownloadParameter>(realmDownloadConfig.Parameters).ToArray<RealmDownloadParameter>();
                string[] strArrays = (
                    from x in (IEnumerable<RealmDownloadParameter>)array1
                    select string.Format("{0}:{1}", x.Name, x.Value)).ToArray<string>();
                this.DoLocalUpdate(realmDownloadConfig.RealmId, strArrays);
            }
        }

        public event EventHandler<RiotUpdateDaemon.UpdaterState[]> Changed;

        public event EventHandler<RiotUpdateDaemon.UpdaterState> Completed;

        public class UpdaterState
        {
            public Process Process;

            public string RealmId;

            public string Status;

            public long Position;

            public long Length;

            public bool Completed;

            private readonly Action notifyChange;

            private readonly Action<RiotUpdateDaemon.UpdaterState> notifyCompleted;

            public UpdaterState()
            {
            }

            private UpdaterState(string realmId, Process process, Action notifyChange, Action<RiotUpdateDaemon.UpdaterState> notifyCompleted)
            {
                this.Process = process;
                this.RealmId = realmId;
                this.notifyChange = notifyChange;
                this.notifyCompleted = notifyCompleted;
                process.Exited += new EventHandler((object sender, EventArgs args) => this.SetCompleted());
                this.TrackProgressAsync().ContinueWith((Task x) => this.SetCompleted());
            }

            internal static RiotUpdateDaemon.UpdaterState Create(string realmId, string[] arguments, Action notifyChange, Action<RiotUpdateDaemon.UpdaterState> notifyCompleted)
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    FileName = Path.Combine(LaunchData.ApplicationDirectory, "riot-update"),
                    Arguments = CommandLine.Escape(arguments),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                return new RiotUpdateDaemon.UpdaterState(realmId, Process.Start(processStartInfo), notifyChange, notifyCompleted);
            }

            private void SetCompleted()
            {
                if (this.Completed)
                {
                    return;
                }
                this.Completed = true;
                this.notifyCompleted(this);
                this.notifyChange();
            }

            private async Task TrackProgressAsync()
            {
                StreamReader standardError = this.Process.StandardError;
                while (true)
                {
                    string str = await standardError.ReadLineAsync();
                    string str1 = str;
                    string str2 = str;
                    if (str1 == null)
                    {
                        break;
                    }
                    GroupCollection groups = RiotUpdateDaemon.RiotUpdaterProgressParser.Match(str2).Groups;
                    string value = groups["position"].Value;
                    string value1 = groups["length"].Value;
                    this.Status = groups["status"].Value;
                    long.TryParse(value, out this.Position);
                    long.TryParse(value1, out this.Length);
                    this.notifyChange();
                }
                this.SetCompleted();
            }

            internal static bool TryCreate(string realmId, string[] arguments, Action notifyChange, Action<RiotUpdateDaemon.UpdaterState> notifyCompleted, out RiotUpdateDaemon.UpdaterState updaterState)
            {
                bool flag;
                try
                {
                    updaterState = RiotUpdateDaemon.UpdaterState.Create(realmId, arguments, notifyChange, notifyCompleted);
                    flag = true;
                }
                catch
                {
                    updaterState = null;
                    flag = false;
                }
                return flag;
            }
        }
    }
}