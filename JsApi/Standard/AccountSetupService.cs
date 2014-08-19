using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using RiotGames.Platform.Gameclient.Domain;
using RiotGames.Platform.Summoner.Masterybook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.JsApi;
using WintermintClient.Riot;
using WintermintData.Storage;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("accountSetup")]
    public class AccountSetupService : JsApiService
    {
        public AccountSetupService()
        {
        }

        private async Task ImportGameSettings()
        {
            AccountSetupService.<ImportGameSettings>d__35 variable = new AccountSetupService.<ImportGameSettings>d__35();
            variable.<>4__this = this;
            variable.<>t__builder = AsyncTaskMethodBuilder.Create();
            variable.<>1__state = -1;
            variable.<>t__builder.Start<AccountSetupService.<ImportGameSettings>d__35>(ref variable);
            return variable.<>t__builder.Task;
        }

        private async Task ImportKeyBindings()
        {
            AccountSetupService.<ImportKeyBindings>d__32 variable = new AccountSetupService.<ImportKeyBindings>d__32();
            variable.<>4__this = this;
            variable.<>t__builder = AsyncTaskMethodBuilder.Create();
            variable.<>1__state = -1;
            variable.<>t__builder.Start<AccountSetupService.<ImportKeyBindings>d__32>(ref variable);
            return variable.<>t__builder.Task;
        }

        private async Task ImportMasteries(RiotAccount account)
        {
            string str;
            Func<MasteryBookPageDTO, string> func = (MasteryBookPageDTO page) => page.PageId.ToString(CultureInfo.InvariantCulture);
            MasteryBookDTO masteryBookDTO = await account.InvokeAsync<MasteryBookDTO>("masteryBookService", "getMasteryBook", account.SummonerId);
            MasteryBookDTO masteryBookDTO1 = masteryBookDTO;
            List<MasteryBookPageDTO> bookPages = masteryBookDTO1.BookPages;
            IOrderedEnumerable<MasteryBookPageDTO> pageId =
                from page in bookPages
                orderby page.PageId
                select page;
            IEnumerable<MasterySetup> talentEntries =
                from page in pageId
                let masteries =
                    from x in page.TalentEntries
                    select new Mastery()
                    {
                        Id = x.TalentId,
                        Rank = x.Rank
                    }
                select new MasterySetup()
                {
                    Id = func(page),
                    Name = page.Name,
                    Masteries = masteries.ToArray<Mastery>()
                };
            List<MasteryBookPageDTO> masteryBookPageDTOs = masteryBookDTO1.BookPages;
            MasteryBookPageDTO masteryBookPageDTO = masteryBookPageDTOs.FirstOrDefault<MasteryBookPageDTO>((MasteryBookPageDTO x) => x.Current);
            if (masteryBookPageDTO != null)
            {
                str = func(masteryBookPageDTO);
            }
            else
            {
                str = null;
            }
            string str1 = str;
            LittleClient client = JsApiService.Client;
            object[] objArray = new object[] { "masteries", null };
            MasteryBook masteryBook = new MasteryBook()
            {
                ActiveId = str1,
                Setups = talentEntries.ToArray<MasterySetup>()
            };
            objArray[1] = masteryBook;
            await client.Invoke("storage.set", objArray);
        }

        [MicroApiMethod("setup")]
        public async Task<AccountSetupService.SetupSummary> Setup(dynamic args, JsApiService.JsResponse progress, JsApiService.JsResponse result)
        {
            //Temp hack
            var func = "";
            var func1 = "";
            var func2 = "";
            func = null;
            func1 = null;
            func2 = null;
            int num = (int)args.handle;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(num);
            Dictionary<string, Task> strs1 = new Dictionary<string, Task>()
            {
                { "runes", Task.FromResult<bool>(true) },
                { "masteries", this.ImportMasteries(riotAccount) },
                { "key-bindings", this.ImportKeyBindings() },
                { "game-settings", this.ImportGameSettings() }
            };
            Dictionary<string, Task> strs2 = strs1;
            Func<AccountSetupService.SetupSummary> func3 = () => {
                AccountSetupService.SetupSummary setupSummary = new AccountSetupService.SetupSummary();
                AccountSetupService.SetupSummary array = setupSummary;
                Dictionary<string, Task> strs = strs2;
                if (func == null)
                {
                    func = (KeyValuePair<string, Task> item) => new { item = item, name = item.Key };
                }
                var collection = strs.Select(func);
                if (func1 == null)
                {
                    func1 = (argument0) => new { <>h__TransparentIdentifier0 = argument0, task = argument0.item.Value };
                }
                var collection1 = collection.Select(func1);
                if (func2 == null)
                {
                    func2 = (argument1) => new AccountSetupService.SetupTask()
                    {
                        Name = argument1.<>h__TransparentIdentifier0.name,
                        Completed = argument1.task.IsCompleted,
                        Faulted = argument1.task.IsFaulted
                    };
                }
                array.Tasks = collection1.Select(func2).ToArray<AccountSetupService.SetupTask>();
                return setupSummary;
            };
            Action<Task> action = (Task _) => progress(func3());
            IEnumerable<Task> values = 
                from x in strs2.Values
                select x.ContinueWith(action);
            try
            {
                await Task.WhenAll(values);
            }
            catch
            {
            }
            await JsApiService.Client.Invoke("mess.flags.firstRun.set");
            return func3();
        }

        public class SetupSummary
        {
            public AccountSetupService.SetupTask[] Tasks;

            public SetupSummary()
            {
            }
        }

        public class SetupTask
        {
            public string Name;

            public bool Completed;

            public bool Faulted;

            public SetupTask()
            {
            }
        }
    }
}