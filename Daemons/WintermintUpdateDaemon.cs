using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.Data;

namespace WintermintClient.Daemons
{
    internal class WintermintUpdateDaemon
    {
        private const string ExecutableName = "wintermint-update";

        private static TimeSpan InitialUpdateDelay;

        private static TimeSpan UpdateDelay;

        static WintermintUpdateDaemon()
        {
            WintermintUpdateDaemon.InitialUpdateDelay = TimeSpan.FromMinutes(5);
            WintermintUpdateDaemon.UpdateDelay = TimeSpan.FromHours(1);
        }

        public WintermintUpdateDaemon()
        {
        }

        public void Initialize()
        {
            this.RunLoop();
        }

        public async Task RunLoop()
        {
            /*
            WintermintUpdateDaemon.<RunLoop>d__0 variable = new WintermintUpdateDaemon.<RunLoop>d__0();
            variable.<>4__this = this;
            variable.<>t__builder = AsyncTaskMethodBuilder.Create();
            variable.<>1__state = -1;
            variable.<>t__builder.Start<WintermintUpdateDaemon.<RunLoop>d__0>(ref variable);
            return variable.<>t__builder.Task;
            */
        }
    }
}