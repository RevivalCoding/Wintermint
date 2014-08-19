using MicroApi;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.Daemons;
using WintermintClient.JsApi;
using WintermintData.Mess;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("mess")]
    public class MessyService : JsApiService
    {
        public MessyService()
        {
        }

        [MicroApiMethod("forceRiotUpdate")]
        public void ForceRiotUpdate()
        {
            Instances.RiotUpdater.TryUpdate();
        }

        [MicroApiMethod("account")]
        public async Task<AccountMess> GetAccountMess()
        {
            return await JsApiService.Client.Invoke<AccountMess>("mess.account");
        }

        [MicroApiMethod("trello")]
        public void OpenTrelloBoard()
        {
            Process.Start("https://trello.com/b/eWEl2gJK");
        }
    }
}