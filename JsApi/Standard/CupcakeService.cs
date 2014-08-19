using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.JsApi;
using WintermintClient.JsApi.ApiHost;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("cupcake")]
    public class CupcakeService : JsApiService
    {
        public CupcakeService()
        {
        }

        [MicroApiMethod("state.get")]
        public async Task<JsonResult> GetLiveGameState(dynamic args)
        {
            string str = (string)args.realm;
            string str1 = (string)args.matchId;
            string str2 = string.Format("https://cupcake.fresh.wintermint.net/{0}/match/{1}/state", str, str1);
            return new JsonResult(await (new WebClient()).DownloadStringTaskAsync(str2));
        }

        [MicroApiMethod("follow")]
        public async Task RequestGameStateTracking(dynamic args)
        {
            string str = (string)args.realm;
            string str1 = (string)args.matchId;
            string str2 = string.Format("https://cupcake.fresh.wintermint.net/{0}/match/{1}/track", str, str1);
            HttpClient httpClient = new HttpClient();
            await httpClient.PostAsync(str2, new StringContent("{ \"notifications\": false }"));
        }
    }
}