using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("store")]
    public class StoreService : JsApiService
    {
        public StoreService()
        {
        }

        [MicroApiMethod("dynamic")]
        public void DynamicInvoke()
        {
            throw new PlatformNotSupportedException();
        }

        [MicroApiMethod("legacy.uri")]
        public async Task<string> GetStoreUri(dynamic args)
        {
            int num = (int)args.handle;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(num);
            return await riotAccount.InvokeAsync<string>("loginService", "getStoreUrl");
        }

        [MicroApiMethod("list")]
        public void ListItems()
        {
            throw new PlatformNotSupportedException();
        }

        [MicroApiMethod("legacy.open")]
        public async Task OpenStore(dynamic args)
        {
            var var;
            StoreService.<OpenStore>(var) variable = new StoreService.<OpenStore>();
            variable.this = this;
            variable.args = args;
            variable.builder = AsyncTaskMethodBuilder.Create();
            variable.state = -1;
            variable.builder.Start<StoreService.<OpenStore>>(ref variable);
            return variable.builder.Task;
        }

        [MicroApiMethod("acquire")]
        public void PurchaseItem()
        {
            throw new PlatformNotSupportedException();
        }
    }
}