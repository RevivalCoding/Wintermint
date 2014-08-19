using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using RiotGames.Platform.Gameclient.Domain;
using RiotGames.Platform.Summoner;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("summoner")]
    public class SummonerService : GameJsApiService
    {
        public SummonerService()
        {
        }

        [MicroApiMethod("create")]
        public async Task<object> Create(dynamic args)
        {
            int num = (int)args.handle;
            string str = (string)args.summonerName;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(num);
            return await riotAccount.InvokeAsync<object>("summonerService", "createDefaultSummoner", str);
        }

        [MicroApiMethod("getName")]
        public async Task<object> GetSummonerName(dynamic args)
        {
            string str = (string)args.realm;
            long num = (long)args.summonerId;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(str);
            long[] numArray = new long[] { num };
            return await riotAccount.InvokeCachedAsync<string[]>("summonerService", "getSummonerNames", numArray);
        }

        [MicroApiMethod("getNameByJid")]
        public Task<string> GetSummonerNameByJid(dynamic args)
        {
            string str = (string)args.realm;
            return JsApiService.GetSummonerNameByJid(str, (string)args.jid);
        }

        [MicroApiMethod("getNames")]
        public async Task<object> GetSummonerNames(dynamic args)
        {
            SummonerService.<GetSummonerNames>d__16 variable = new SummonerService.<GetSummonerNames>d__16();
            variable.<>4__this = this;
            variable.args = args;
            variable.<>t__builder = AsyncTaskMethodBuilder<object>.Create();
            variable.<>1__state = -1;
            variable.<>t__builder.Start<SummonerService.<GetSummonerNames>d__16>(ref variable);
            return variable.<>t__builder.Task;
        }

        [MicroApiMethod("getSummoner")]
        public async Task<object> JsGetSummoner(dynamic args)
        {
            object variable;
            string str = (string)args.summonerName;
            string str1 = (string)args.realm;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(str1);
            PublicSummoner publicSummoner = await riotAccount.InvokeCachedAsync<PublicSummoner>("summonerService", "getSummonerByName", str);
            if (publicSummoner != null)
            {
                variable = new { AccountId = publicSummoner.AccountId, SummonerId = publicSummoner.SummonerId, SummonerName = publicSummoner.Name, InternalName = publicSummoner.InternalName, Level = publicSummoner.SummonerLevel, IconId = publicSummoner.ProfileIconId };
            }
            else
            {
                riotAccount.RemoveCached<PublicSummoner>("summonerService", "getSummonerByName", str);
                variable = null;
            }
            return variable;
        }
    }
}