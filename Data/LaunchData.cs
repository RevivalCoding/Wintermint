using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace WintermintClient.Data
{
    internal static class LaunchData
    {
        private static int loadAttempts;

        public static string ApplicationDirectory
        {
            get;
            private set;
        }

        private static string ApplicationReleasesDirectory
        {
            get;
            set;
        }

        public static string DataDirectory
        {
            get;
            private set;
        }

        public static string LauncherExecutable
        {
            get;
            private set;
        }

        public static string RiotContainerDirectory
        {
            get;
            private set;
        }

        public static string RootDirectory
        {
            get;
            private set;
        }

        public static string WintermintRootExecutable
        {
            get;
            private set;
        }

        static LaunchData()
        {
        }

        public static string GetTemporaryFolder(string purpose)
        {
            string str = string.Format("{0:N}{1:N}", Guid.NewGuid(), Guid.NewGuid());
            return Path.Combine(Path.GetTempPath(), str);
        }

        public static void Initialize()
        {
            if (Interlocked.Increment(ref LaunchData.loadAttempts) > 1)
            {
                return;
            }
            DirectoryInfo directory = (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory;
            LaunchData.ApplicationDirectory = directory.FullName;
            DirectoryInfo parent = directory.Parent;
            LaunchData.ApplicationReleasesDirectory = parent.FullName;
            LaunchData.RootDirectory = parent.Parent.FullName;
            LaunchData.DataDirectory = Path.Combine(LaunchData.RootDirectory, "data");
            LaunchData.RiotContainerDirectory = Path.Combine(LaunchData.RootDirectory, "game");
            LaunchData.LauncherExecutable = Path.Combine(LaunchData.RootDirectory, "launcher");
            LaunchData.WintermintRootExecutable = Path.Combine(LaunchData.RootDirectory, "wintermint.exe");
            Directory.CreateDirectory(LaunchData.ApplicationDirectory);
            Directory.CreateDirectory(LaunchData.ApplicationReleasesDirectory);
            Directory.CreateDirectory(LaunchData.RootDirectory);
            Directory.CreateDirectory(LaunchData.DataDirectory);
            Directory.CreateDirectory(LaunchData.RiotContainerDirectory);
            if ((!"application".Equals((new DirectoryInfo(LaunchData.ApplicationReleasesDirectory)).Name, StringComparison.OrdinalIgnoreCase) || !"data".Equals((new DirectoryInfo(LaunchData.DataDirectory)).Name, StringComparison.OrdinalIgnoreCase) || !File.Exists(LaunchData.LauncherExecutable) || !File.Exists(LaunchData.WintermintRootExecutable) ? true : !File.Exists(Path.Combine(LaunchData.ApplicationDirectory, "clean"))))
            {
                MessageBox.Show("Please re-install Wintermint. Some important files and folders are missing.", "Wintermint", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                Environment.Exit(0);
            }
        }

        public static void Launch(string name, string arguments = null)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = LaunchData.LauncherExecutable,
                Arguments = string.Format("{0} {1}", name, arguments),
                UseShellExecute = false
            };
            Process.Start(processStartInfo);
        }

        public static void LaunchLocal(string name, string arguments = null)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(LaunchData.ApplicationDirectory, name),
                Arguments = arguments,
                UseShellExecute = false
            };
            Process.Start(processStartInfo);
        }

        public static bool TryLaunch(string name, string arguments = null)
        {
            bool flag;
            try
            {
                LaunchData.Launch(name, arguments);
                flag = true;
            }
            catch
            {
                flag = false;
            }
            return flag;
        }

        public static bool TryLaunchLocal(string name, string arguments = null)
        {
            bool flag;
            try
            {
                LaunchData.LaunchLocal(name, arguments);
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