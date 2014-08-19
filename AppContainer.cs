using Browser;
using Browser.BrowserWindows;
using Browser.Rpc;
using FileDatabase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WintermintClient.JsApi;
using WintermintClient.JsApi.ApiHost;
using WintermintClient.Util;

namespace WintermintClient
{
    internal class AppContainer
    {
        public ManualResetEvent CloseHandle = new ManualResetEvent(false);

        public IBrowserWindow window;

        private WintermintApiHost api;

        private Dictionary<string, string> mimeTypes;

        private Dictionary<string, AppContainer.SchemeFileDbLink> schemeMap;

        private readonly static AppContainer.SchemeFileDbLink[] WintermintSchemes;

        static AppContainer()
        {
            AppContainer.SchemeFileDbLink[] schemeFileDbLink = new AppContainer.SchemeFileDbLink[] { new AppContainer.SchemeFileDbLink("astral", "support", "http/ui/"), new AppContainer.SchemeFileDbLink("astral-data", "support", ""), new AppContainer.SchemeFileDbLink("astral-media", "media", "") };
            AppContainer.WintermintSchemes = schemeFileDbLink;
        }

        public AppContainer()
        {
        }

        private string GetExtension(string path)
        {
            int num = path.LastIndexOf('/');
            int num1 = path.LastIndexOf('.');
            if (num1 <= num)
            {
                return "";
            }
            return path.Substring(num1 + 1);
        }

        private string GetMimeType(string extension)
        {
            string str;
            if (this.mimeTypes.TryGetValue(extension, out str))
            {
                return str;
            }
            return this.mimeTypes["default"];
        }

        public void Initialize()
        {
            AppContainer.InitializeDebugEnvironment();
            Process.GetCurrentProcess().PriorityBoostEnabled = true;
            string[] array = (
                from x in (IEnumerable<AppContainer.SchemeFileDbLink>)AppContainer.WintermintSchemes
                select x.DatabaseName).ToArray<string>();
            Instances.InitializeAsync(array).Wait();
            this.schemeMap = ((IEnumerable<AppContainer.SchemeFileDbLink>)AppContainer.WintermintSchemes).ToDictionary<AppContainer.SchemeFileDbLink, string>((AppContainer.SchemeFileDbLink x) => x.Scheme);
            string str = Instances.SupportFiles.GetString("http/mimetypes.json");
            this.mimeTypes = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);
            BrowserEngine.DataRequest += new EventHandler<DataRequest>(this.OnDataRequest);
            JsApiService.Push = (string key, object obj) => PushNotification.Send(this.window.CefBrowser, key, obj);
            JsApiService.PushJson = (string key, string json) => PushNotification.SendJson(this.window.CefBrowser, key, json);
            this.window = BrowserWindowFactory.Create();
            this.window.RequestReceived += new EventHandler<RequestContext>((object sender, RequestContext context) => this.api.ProcessRequest(context));
            this.window.BrowserClosed += new EventHandler((object sender, EventArgs args) => this.CloseHandle.Set());
            this.window.BrowserCreated += new EventHandler((object sender, EventArgs args) =>
            {
                Instances.WindowHandle = this.window.Handle;
                AppUserModelId.SetWintermintProperties(this.window.Handle);
                this.api = new WintermintApiHost(this.window);
            });
        }

        private static void InitializeDebugEnvironment()
        {
            Console.Out.WriteLineAsync("::host.require = ~> 0.0.1");
            Console.Out.WriteLineAsync("::host.generate_reports = all");
            Console.Out.WriteLineAsync("::host.plugin_additional_paths = /home/kevin/env/wintermint/debug-plugins");
            Console.Out.WriteLineAsync("::host.plugin_autoload = true");
            Console.Out.WriteLineAsync("::host.source_map = /home/kevin/code/wintermint-ui/debugging/symbols.bin");
            Console.Out.WriteLineAsync("::host.league_path = /home/kevin/env/wintermint/league-of-legends/na/");
        }

        private async void OnDataRequest(object sender, DataRequest request)
        {
            try
            {
                await this.ProcessDataRequest(request);
            }
            catch (Exception exception)
            {
                request.SetNoData();
            }
        }

        private async Task ProcessDataRequest(DataRequest request)
        {
            request.Headers.Add("Access-Control-Allow-Origin", "astral://prototype");
            Uri uri = new Uri(request.Url);
            AppContainer.SchemeFileDbLink item = this.schemeMap[uri.Scheme];
            string str = string.Concat(item.PathPrefix, uri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
            string mimeType = this.GetMimeType(this.GetExtension(str));
            IFileDb fileDb = Instances.FileDatabases[item.DatabaseName];
            using (Stream stream = fileDb.GetStream(str.ToLowerInvariant()))
            {
                MemoryStream memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Seek((long)0, SeekOrigin.Begin);
                request.SetData(memoryStream, mimeType);
            }
        }

        private struct SchemeFileDbLink
        {
            public readonly string Scheme;

            public readonly string DatabaseName;

            public readonly string PathPrefix;

            public SchemeFileDbLink(string scheme, string databaseName, string pathPrefix)
            {
                this.Scheme = scheme;
                this.DatabaseName = databaseName;
                this.PathPrefix = pathPrefix;
            }
        }
    }
}