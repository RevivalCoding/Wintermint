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
    [MicroApiService("riot")]
    public class RiotService : JsApiService
    {
        public RiotService()
        {
        }

        [MicroApiMethod("storeUri")]
        public async Task<string> GetStoreUri(dynamic args)
        {
            int num = (int)args.handle;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(num);
            return await riotAccount.InvokeAsync<string>("loginService", "getStoreUrl");
        }

        [MicroApiMethod("isOnline")]
        public async Task<bool> IsOnline(dynamic args)
        {
            RiotAccount riotAccount = JsApiService.RiotAccount;
            bool flag = await riotAccount.InvokeAsync<bool>("loginService", "isLoggedIn", (string)args.username);
            return flag;
        }
    }
}