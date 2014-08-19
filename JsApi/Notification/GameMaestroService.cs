using MicroApi;
using RiotGames.Platform.Game;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WintermintClient.Data;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Notification
{
    [MicroApiSingleton]
    public class GameMaestroService : JsApiService
    {
        public GameMaestroService()
        {
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                account.MessageReceived += new EventHandler<MessageReceivedEventArgs>(this.OnFlexMessageReceived);
                account.InvocationResult += new EventHandler<InvocationResultEventArgs>(this.OnInvocationResult);
            });
            JsApiService.AccountBag.AccountRemoved += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                account.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(this.OnFlexMessageReceived);
                account.InvocationResult -= new EventHandler<InvocationResultEventArgs>(this.OnInvocationResult);
            });
        }

        private static Task CopyRadsDependenciesAsync(string realmId)
        {
            string radsDirectory = GameMaestroService.GetRadsDirectory(realmId);
            string latestDeploy = GameMaestroService.GetLatestDeploy(realmId, "projects", "lol_launcher");
            string str = Path.Combine(latestDeploy, "riotradsio.dll");
            string str1 = Path.Combine(radsDirectory, "riotradsio.dll");
            TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
            ThreadPool.QueueUserWorkItem((object state) =>
            {
                try
                {
                    try
                    {
                        File.Copy(str, str1, true);
                    }
                    catch
                    {
                    }
                }
                finally
                {
                    taskCompletionSource.SetResult(null);
                }
            });
            return taskCompletionSource.Task;
        }

        private static string GetLatest(string directory)
        {
            IEnumerable<string> directories =
                from dir in Directory.GetDirectories(directory)
                let name = (new DirectoryInfo(dir)).Name
                where ReleasePackage.IsVersionString(name)
                orderby (new ReleasePackage(name)).Version descending
                select dir;
            return directories.First<string>();
        }

        private static string GetLatestDeploy(string realmId, string category, string name)
        {
            string radsDirectory = GameMaestroService.GetRadsDirectory(realmId);
            string str = Path.Combine(radsDirectory, category, name, "releases");
            return Path.Combine(GameMaestroService.GetLatest(str), "deploy");
        }

        private static string GetRadsDirectory(string realmId)
        {
            string riotContainerDirectory = LaunchData.RiotContainerDirectory;
            string str = Path.Combine(riotContainerDirectory, string.Concat("league#", realmId));
            return Path.Combine(str, "RADS");
        }

        private void OnData(RiotAccount account, object message)
        {
            this.OnDataInternal(account, message);
        }

        private async Task OnDataInternal(RiotAccount account, object message)
        {
            PlayerCredentialsDto playerCredentialsDto = message as PlayerCredentialsDto;
            if (playerCredentialsDto != null)
            {
                JsApiService.PushIfActive(account, "game:launch", null);
                if (!await GameMaestroService.TryStartGame(account.RealmId, playerCredentialsDto))
                {
                    JsApiService.Push("game:launch:fail", null);
                }
            }
        }

        private void OnFlexMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            this.OnData(sender as RiotAccount, e.Body);
        }

        private void OnInvocationResult(object sender, InvocationResultEventArgs args)
        {
            this.OnData(sender as RiotAccount, args.Result);
        }

        private static async Task RunLeagueOfLegends(string realmId, string arguments)
        {
            await GameMaestroService.CopyRadsDependenciesAsync(realmId);
            string latestDeploy = GameMaestroService.GetLatestDeploy(realmId, "solutions", "lol_game_client_sln");
            string str = GameMaestroService.GetLatestDeploy(realmId, "projects", "lol_game_client");
            Process process = new Process();
            Process process1 = process;
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = string.Format("{0}/League of Legends.exe", str),
                Arguments = arguments,
                WorkingDirectory = latestDeploy,
                UseShellExecute = false
            };
            process1.StartInfo = processStartInfo;
            process.Start();
        }

        public static Task StartGame(string realmId, PlayerCredentialsDto game)
        {
            if (game == null)
            {
                return Task.FromResult<bool>(true);
            }
            JsApiService.Push("game:reconnect", null);
            object[] serverIp = new object[] { game.ServerIp, game.ServerPort, game.EncryptionKey, game.SummonerId };
            return GameMaestroService.RunLeagueOfLegends(realmId, string.Format("\"56471\" \"wintermint-delegator\" \"wintermint-delegator\" \"{0} {1} {2} {3}\"", serverIp));
        }

        public static Task StartSpectatorGame(string realmId, string platformId, PlayerCredentialsDto game)
        {
            if (game == null)
            {
                return Task.FromResult<bool>(true);
            }
            JsApiService.Push("game:reconnect", null);
            object[] observerServerIp = new object[] { game.ObserverServerIp, game.ObserverServerPort, game.ObserverEncryptionKey, game.GameId, platformId };
            return GameMaestroService.RunLeagueOfLegends(realmId, string.Format("\"56471\" \"wintermint-delegator\" \"wintermint-delegator\" \"spectator {0}:{1} {2} {3} {4}\"", observerServerIp));
        }

        public static async Task<bool> TryStartGame(string realmId, PlayerCredentialsDto game)
        {
            bool flag;
            try
            {
                await GameMaestroService.StartGame(realmId, game);
                flag = true;
            }
            catch
            {
                flag = false;
            }
            return flag;
        }

        public static async Task<bool> TryStartSpectatorGame(string realmId, string platformId, PlayerCredentialsDto game)
        {
            bool flag;
            try
            {
                await GameMaestroService.StartSpectatorGame(realmId, platformId, game);
                flag = true;
            }
            catch
            {
                flag = false;
            }
            return flag;
        }
    }
}