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
            StoreService.<OpenStore>d__f variable = new StoreService.<OpenStore>d__f();
            variable.<>4__this = this;
            variable.args = args;
            variable.<>t__builder = AsyncTaskMethodBuilder.Create();
            variable.<>1__state = -1;
            variable.<>t__builder.Start<StoreService.<OpenStore>d__f>(ref variable);
            return variable.<>t__builder.Task;
        }

        [MicroApiMethod("acquire")]
        public void PurchaseItem()
        {
            throw new PlatformNotSupportedException();
        }
    }
}