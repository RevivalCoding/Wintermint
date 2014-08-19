using FileDatabase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.Daemons;
using WintermintClient.Data;
using WintermintClient.Riot;

namespace WintermintClient
{
    internal static class Instances
    {
        public static LittleClient Client;

        public static RiotAccountBag AccountBag;

        public static Dictionary<string, IFileDb> FileDatabases;

        public static IFileDb SupportFiles;

        public static IFileDb MediaFiles;

        public static WintermintUpdateDaemon WintermintUpdater;

        public static RiotUpdateDaemon RiotUpdater;

        public static IntPtr WindowHandle;

        static Instances()
        {
            Instances.Client = new LittleClient();
            Instances.AccountBag = new RiotAccountBag();
            Instances.WintermintUpdater = new WintermintUpdateDaemon();
            Instances.RiotUpdater = new RiotUpdateDaemon();
        }

        public static async Task InitializeAsync(string[] fileDatabases)
        {
            bool flag;
            LaunchData.Initialize();
            Instances.FileDatabases = new Dictionary<string, IFileDb>(StringComparer.OrdinalIgnoreCase);
            string[] strArrays = fileDatabases;
            for (int i = 0; i < (int)strArrays.Length; i++)
            {
                string str = strArrays[i];
                Instances.FileDatabases[str] = FileDbFactory.Open(str);
            }
            Instances.SupportFiles = Instances.FileDatabases["support"];
            Instances.MediaFiles = Instances.FileDatabases["media"];
            Instances.WintermintUpdater.Initialize();
            Instances.RiotUpdater.Initialize();
            ChatStateTransformData.Initialize();
            Task[] taskArray = new Task[] { GameData.Initialize(Instances.SupportFiles), ChampionNameData.Initialize(Instances.SupportFiles), PracticeGameData.Initialize(Instances.SupportFiles), RuneData.Initialize(Instances.SupportFiles), ReplayData.Initialize(Instances.SupportFiles) };
            await Task.WhenAll(taskArray);
        }
    }
}