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
            WintermintUpdateDaemon.<RunLoop> variable = new WintermintUpdateDaemon.<RunLoop>();
            variable.this = this;
            variable.builder = AsyncTaskMethodBuilder.Create();
            variable.state = -1;
            variable.builder.Start<WintermintUpdateDaemon.<RunLoop>(ref variable);
            return variable.builder.Task;
        }
    }
}