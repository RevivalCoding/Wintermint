using MicroApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.JsApi;
using WintermintData.Riot;

namespace WintermintClient.JsApi.Standard.Riot
{
    [MicroApiService("riot")]
    public class RealmService : JsApiService
    {
        public RealmService()
        {
        }

        public static Task<SupportedRealm[]> GetRealms()
        {
            return JsApiService.Client.InvokeCached<SupportedRealm[]>("riot.realms.get");
        }

        [MicroApiMethod("realms")]
        public static async Task<object> GetRealmsClient()
        {
            SupportedRealm[] supportedRealmArray = await JsApiService.Client.InvokeCached<SupportedRealm[]>("riot.realms.get");
            object name =
                from x in (IEnumerable<SupportedRealm>)supportedRealmArray
                orderby x.Name
                select x;
            return name;
        }
    }
}