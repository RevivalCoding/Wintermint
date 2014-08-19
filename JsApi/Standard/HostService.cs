using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using WintermintClient;
using WintermintClient.JsApi;
using WintermintClient.Native;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("host")]
    public class HostService : JsApiService
    {
        public HostService()
        {
        }

        [MicroApiMethod("openUrl")]
        public void OpenUri(dynamic args)
        {
            Process.Start((string)args.href);
        }

        [MicroApiMethod("spotlight.pulse")]
        [MicroApiMethod("spotlight.start")]
        public void Pulse()
        {
            WindowFlasher.Pulse(Instances.WindowHandle);
        }

        [MicroApiMethod("spotlight")]
        public void Spotlight(dynamic args)
        {
            WindowFlasher.Flash(Instances.WindowHandle, (args.count == (object)null ? 1 : (int)args.count));
        }

        [MicroApiMethod("spotlight.stop")]
        public void Stop()
        {
            WindowFlasher.Stop(Instances.WindowHandle);
        }
    }
}