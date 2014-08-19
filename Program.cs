using Browser;
using System;
using System.IO;
using System.Windows.Forms;
using WintermintClient.Data;

namespace WintermintClient
{
    public class Program
    {
        public Program()
        {
        }

        [STAThread]
        public static void Main(string[] args)
        {
            if (!BrowserEngine.Initialize(args))
            {
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (IDisposable disposable = Program.TryAcquireLock())
            {
                AppContainer appContainer = new AppContainer();
                appContainer.Initialize();
                Application.Run(appContainer.window as Form);
            }
            BrowserEngine.Shutdown();
        }

        private static IDisposable TryAcquireLock()
        {
            IDisposable fileStream;
            LaunchData.Initialize();
            string str = Path.Combine(LaunchData.DataDirectory, "run-lock");
            try
            {
                fileStream = new FileStream(str, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            }
            catch
            {
                MessageBox.Show("Wintermint is already running or is shutting down (sorry).\n\nI will do my best to remove this message soon.\n\n-- astralfoxy", "Wintermint", MessageBoxButtons.OK);
                Environment.Exit(0);
                fileStream = null;
            }
            return fileStream;
        }
    }
}