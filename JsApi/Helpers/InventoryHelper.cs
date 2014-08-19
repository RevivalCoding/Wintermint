using Newtonsoft.Json.Linq;
using RiotGames.Platform.Gameclient.Domain;
using RiotGames.Platform.Summoner;
using RiotGames.Platform.Summoner.Masterybook;
using RiotGames.Platform.Summoner.Spellbook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.Data;
using WintermintClient.JsApi;
using WintermintClient.Riot;
using WintermintData.Storage;

namespace WintermintClient.JsApi.Helpers
{
    internal class InventoryHelper : JsApiService
    {
        private const int RunesPerType = 9;

        private const int MaximumMasteryPages = 20;

        public static InventoryHelper Instance;

        static InventoryHelper()
        {
            InventoryHelper.Instance = new InventoryHelper();
        }

        public InventoryHelper()
        {
        }

        private static void ClearMasteryCache(RiotAccount account)
        {
            InventoryHelper.ClearMasteryCache(account, account.SummonerId);
        }

        private static void ClearMasteryCache(RiotAccount account, long summonerId)
        {
            account.RemoveCached<MasteryBookDTO>("masteryBookService", "getMasteryBook", summonerId);
            JsApiService.Client.Purge<MasteryBook>("storage.get", new object[] { "masteries" });
        }

        private static void ClearRuneCache(RiotAccount account)
        {
            InventoryHelper.ClearRuneCache(account, account.SummonerId);
        }

        private static void ClearRuneCache(RiotAccount account, long summonerId)
        {
            account.RemoveCached<SpellBookDTO>("spellBookService", "getSpellBook", summonerId);
        }

        private static RiotAccount GetAccount(string realm, string summonerName)
        {
            RiotAccount[] all = JsApiService.AccountBag.GetAll();
            return all.First<RiotAccount>((RiotAccount x) =>
            {
                if (!string.Equals(x.RealmId, realm, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                return string.Equals(x.SummonerName, summonerName, StringComparison.OrdinalIgnoreCase);
            });
        }

        public static async Task<object> GetMasterySetups(string realm, string summonerName)
        {
            PublicSummoner summoner = await JsApiService.GetSummoner(realm, summonerName);
            return await InventoryHelper.GetMasterySetups(realm, (double)summoner.SummonerId);
        }

        public static async Task<object> GetMasterySetups(string realm, double summonerId)
        {
            object variable;
            RiotAccount[] all = JsApiService.AccountBag.GetAll();
            bool flag = all.Any<RiotAccount>((RiotAccount x) =>
            {
                if (!string.Equals(x.RealmId, realm, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                return (double)x.SummonerId == summonerId;
            });
            if (!flag)
            {
                RiotAccount riotAccount = JsApiService.AccountBag.Get(realm);
                MasteryBookDTO masteryBookDTO = await riotAccount.InvokeCachedAsync<MasteryBookDTO>("masteryBookService", "getMasteryBook", summonerId);
                MasteryBookDTO masteryBookDTO1 = masteryBookDTO;
                List<MasteryBookPageDTO> bookPages = masteryBookDTO1.BookPages;
                MasteryBookPageDTO masteryBookPageDTO = bookPages.FirstOrDefault<MasteryBookPageDTO>((MasteryBookPageDTO x) => x.Current);
                if (masteryBookPageDTO == null)
                {
                    masteryBookPageDTO = new MasteryBookPageDTO();
                }
                string str = masteryBookPageDTO.PageId.ToString(CultureInfo.InvariantCulture);
                List<MasteryBookPageDTO> masteryBookPageDTOs = masteryBookDTO1.BookPages;
                IOrderedEnumerable<MasteryBookPageDTO> pageId =
                    from page in masteryBookPageDTOs
                    orderby page.PageId
                    select page;
                variable = new
                {
                    Local = false,
                    ActiveId = str,
                    Setups =
                        from page in pageId
                        select new
                        {
                            Id = page.PageId.ToString(CultureInfo.InvariantCulture),
                            Name = page.Name,
                            Masteries =
                                from entry in page.TalentEntries
                                select new { Id = entry.TalentId, Points = entry.Rank }
                        }
                };
            }
            else
            {
                LittleClient client = JsApiService.Client;
                object[] objArray = new object[] { "masteries" };
                MasteryBook masteryBook = await client.InvokeCached<MasteryBook>("storage.get", objArray);
                string activeId = masteryBook.ActiveId;
                MasterySetup[] setups = masteryBook.Setups;
                variable = new
                {
                    Local = true,
                    ActiveId = activeId,
                    Setups =
                        from setup in (IEnumerable<MasterySetup>)setups
                        select new
                        {
                            Id = setup.Id,
                            Name = setup.Name,
                            Masteries =
                                from mastery in (IEnumerable<Mastery>)setup.Masteries
                                select new { Id = mastery.Id, Points = mastery.Rank }
                        }
                };
            }
            return variable;
        }

        public static async Task<object> GetRuneSetups(string realm, string summonerName)
        {
            PublicSummoner summoner = await JsApiService.GetSummoner(realm, summonerName);
            return await InventoryHelper.GetRuneSetups(realm, (double)summoner.SummonerId);
        }

        public static async Task<object> GetRuneSetups(string realm, double summonerId)
        {
            RiotAccount[] all = JsApiService.AccountBag.GetAll();
            RiotAccount riotAccount = JsApiService.AccountBag.Get(realm);
            SpellBookDTO spellBookDTO = await riotAccount.InvokeCachedAsync<SpellBookDTO>("spellBookService", "getSpellBook", summonerId);
            SpellBookDTO spellBookDTO1 = spellBookDTO;
            List<SpellBookPageDTO> bookPages = spellBookDTO1.BookPages;
            SpellBookPageDTO spellBookPageDTO = bookPages.FirstOrDefault<SpellBookPageDTO>((SpellBookPageDTO x) => x.Current);
            if (spellBookPageDTO == null)
            {
                spellBookPageDTO = new SpellBookPageDTO();
            }
            SpellBookPageDTO spellBookPageDTO1 = spellBookPageDTO;
            bool flag = all.Any<RiotAccount>((RiotAccount x) =>
            {
                if (!string.Equals(x.RealmId, realm, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                return (double)x.SummonerId == summonerId;
            });
            double pageId = spellBookPageDTO1.PageId;
            List<SpellBookPageDTO> spellBookPageDTOs = spellBookDTO1.BookPages;
            IOrderedEnumerable<SpellBookPageDTO> pageId1 =
                from page in spellBookPageDTOs
                orderby page.PageId
                select page;
            object variable = new
            {
                Local = flag,
                ActiveId = pageId,
                Setups =
                    from page in pageId1
                    select new
                    {
                        Id = page.PageId,
                        Name = page.Name,
                        Runes =
                            from entry in page.SlotEntries
                            group entry by entry.RuneId into entries
                            select new { Id = entries.Key, Count = entries.Count<SlotEntry>() }
                    }
            };
            return variable;
        }

        private static RuneType GetRuneType(int id)
        {
            Rune rune;
            if (!RuneData.Runes.TryGetValue(id, out rune))
            {
                return RuneType.Red;
            }
            return rune.Type;
        }

        private static void SaveRiotMasteryBook(MasteryBook book)
        {
            MasterySetup[] array = (book.Setups ?? new MasterySetup[0]).Take<MasterySetup>(20).ToArray<MasterySetup>();
            MasteryBookPageDTO[] masteryBookPageDTOArray = array.Select<MasterySetup, MasteryBookPageDTO>(new Func<MasterySetup, MasteryBookPageDTO>(InventoryHelper.ToMasteryBookPage)).ToArray<MasteryBookPageDTO>();
            Dictionary<string, string[]> dictionary = (
                from x in (IEnumerable<MasteryBookPageDTO>)masteryBookPageDTOArray
                select x.Name).ToLookup<string, string>((string x) => x).ToDictionary<IGrouping<string, string>, string, string[]>((IGrouping<string, string> x) => x.Key, (IGrouping<string, string> x) => x.ToArray<string>());
            for (int i = 0; i < (int)masteryBookPageDTOArray.Length; i++)
            {
                MasteryBookPageDTO masteryBookPageDTO = masteryBookPageDTOArray[i];
                masteryBookPageDTO.PageId = (double)(i + 1);
                if (string.IsNullOrEmpty(masteryBookPageDTO.Name) || (int)dictionary[masteryBookPageDTO.Name].Length > 1)
                {
                    masteryBookPageDTO.Name = string.Format("#{0:00}. {1}", i, masteryBookPageDTO.Name);
                }
                if (((IEnumerable<MasteryBookPageDTO>)masteryBookPageDTOArray).All<MasteryBookPageDTO>((MasteryBookPageDTO x) => !x.Current) && array[i].Id == book.ActiveId)
                {
                    masteryBookPageDTO.Current = true;
                }
            }
            if ((int)masteryBookPageDTOArray.Length > 0)
            {
                if (((IEnumerable<MasteryBookPageDTO>)masteryBookPageDTOArray).All<MasteryBookPageDTO>((MasteryBookPageDTO x) => !x.Current))
                {
                    masteryBookPageDTOArray[0].Current = true;
                }
            }
            RiotAccount[] all = JsApiService.AccountBag.GetAll();
            for (int j = 0; j < (int)all.Length; j++)
            {
                RiotAccount riotAccount = all[j];
                MasteryBookDTO masteryBookDTO = new MasteryBookDTO()
                {
                    SummonerId = (double)riotAccount.SummonerId,
                    BookPages = masteryBookPageDTOArray.ToList<MasteryBookPageDTO>()
                };
                riotAccount.InvokeAsync<object>("masteryBookService", "saveMasteryBook", masteryBookDTO);
                InventoryHelper.ClearMasteryCache(riotAccount);
            }
        }

        public static Task SetActiveMasterySetup(JObject jObject)
        {
            return InventoryHelper.SetActiveMasterySetup(InventoryHelper.ToMasterySetup(jObject));
        }

        public static async Task SetActiveMasterySetup(MasterySetup setup)
        {
            bool flag;
            JsApiService.Client.Invoke("storage.masteries.active.set", setup.Id);
            LittleClient client = JsApiService.Client;
            object[] objArray = new object[] { "masteries" };
            MasteryBook id = await client.InvokeCached<MasteryBook>("storage.get", objArray);
            IEnumerable<MasterySetup> masterySetups = id.Setups.Take<MasterySetup>(19);
            MasterySetup[] masterySetupArray = new MasterySetup[] { setup };
            MasterySetup[] array = masterySetups.Concat<MasterySetup>(masterySetupArray).ToArray<MasterySetup>();
            for (int i = 0; i < (int)array.Length; i++)
            {
                array[i].Id = i.ToString(CultureInfo.InvariantCulture);
            }
            setup.Name = "[active]";
            id.Setups = array;
            id.ActiveId = setup.Id;
            InventoryHelper.SaveRiotMasteryBook(id);
            RiotAccount[] all = JsApiService.AccountBag.GetAll();
            for (int j = 0; j < (int)all.Length; j++)
            {
                InventoryHelper.ClearMasteryCache(all[j]);
            }
        }

        public static async Task SetActiveRuneSetup(string realm, string summonerName, JObject jSetup)
        {
            SpellBookPageDTO spellBookPage = InventoryHelper.ToSpellBookPage(InventoryHelper.ToRuneSetup(jSetup));
            RiotAccount account = InventoryHelper.GetAccount(realm, summonerName);
            await account.InvokeAsync<object>("spellBookService", "selectDefaultSpellBookPage", spellBookPage);
            InventoryHelper.ClearRuneCache(account);
        }

        public static async Task SetMasterySetups(JObject jObject)
        {
            bool flag;
            string obj = jObject["activeId"].ToObject<string>();
            MasterySetup[] array = jObject["setups"].Select<JToken, MasterySetup>(new Func<JToken, MasterySetup>(InventoryHelper.ToMasterySetup)).ToArray<MasterySetup>();
            MasteryBook masteryBook = new MasteryBook()
            {
                ActiveId = obj,
                Setups = array
            };
            MasteryBook masteryBook1 = masteryBook;
            LittleClient client = JsApiService.Client;
            object[] objArray = new object[] { "masteries", masteryBook1 };
            await client.Invoke("storage.set", objArray);
            RiotAccount[] all = JsApiService.AccountBag.GetAll();
            for (int i = 0; i < (int)all.Length; i++)
            {
                InventoryHelper.ClearMasteryCache(all[i]);
            }
            InventoryHelper.SaveRiotMasteryBook(masteryBook1);
        }

        public static async Task SetRuneSetups(string realm, string summonerName, JToken obj)
        {
            RiotAccount account = InventoryHelper.GetAccount(realm, summonerName);
            string item = (string)obj["activeId"];
            List<SpellBookPageDTO> list = obj["setups"].Select<JToken, InventoryHelper.RuneSetup>(new Func<JToken, InventoryHelper.RuneSetup>(InventoryHelper.ToRuneSetup)).Select<InventoryHelper.RuneSetup, SpellBookPageDTO>(new Func<InventoryHelper.RuneSetup, SpellBookPageDTO>(InventoryHelper.ToSpellBookPage)).ToList<SpellBookPageDTO>();
            List<SpellBookPageDTO> spellBookPageDTOs = list;
            IEnumerable<double> pageId =
                from x in spellBookPageDTOs
                select x.PageId;
            double[] array = (
                from x in pageId
                orderby x
                select x).ToArray<double>();
            for (int i = 0; i < (int)array.Length; i++)
            {
                list[i].PageId = array[i];
            }
            List<SpellBookPageDTO> spellBookPageDTOs1 = list;
            IEnumerable<string> name =
                from x in spellBookPageDTOs1
                select x.Name;
            ILookup<string, string> lookup = name.ToLookup<string, string>((string x) => x);
            Func<IGrouping<string, string>, string> key = (IGrouping<string, string> x) => x.Key;
            Dictionary<string, string[]> dictionary = lookup.ToDictionary<IGrouping<string, string>, string, string[]>(key, (IGrouping<string, string> x) => x.ToArray<string>());
            for (int j = 0; j < list.Count; j++)
            {
                SpellBookPageDTO summonerId = list[j];
                summonerId.SummonerId = (double)account.SummonerId;
                if (string.IsNullOrEmpty(summonerId.Name) || (int)dictionary[summonerId.Name].Length > 1)
                {
                    summonerId.Name = string.Format("#{0:00}. {1}", j, summonerId.Name);
                }
                if (summonerId.PageId.ToString(CultureInfo.InvariantCulture) == item)
                {
                    summonerId.Current = true;
                }
            }
            if (list.Count > 0)
            {
                List<SpellBookPageDTO> spellBookPageDTOs2 = list;
                if (spellBookPageDTOs2.All<SpellBookPageDTO>((SpellBookPageDTO x) => !x.Current))
                {
                    list[0].Current = true;
                }
            }
            RiotAccount riotAccount = account;
            SpellBookDTO spellBookDTO = new SpellBookDTO()
            {
                SummonerId = (double)account.SummonerId,
                BookPages = list
            };
            await riotAccount.InvokeAsync<object>("spellBookService", "saveSpellBook", spellBookDTO);
            InventoryHelper.ClearRuneCache(account);
        }

        private static MasteryBookPageDTO ToMasteryBookPage(MasterySetup setup)
        {
            IEnumerable<TalentEntry> masteries =
                from x in (IEnumerable<Mastery>)setup.Masteries
                select new TalentEntry()
                {
                    TalentId = x.Id,
                    Rank = x.Rank
                };
            MasteryBookPageDTO masteryBookPageDTO = new MasteryBookPageDTO()
            {
                Name = setup.Name,
                TalentEntries = masteries.ToList<TalentEntry>(),
                Current = false
            };
            return masteryBookPageDTO;
        }

        private static MasterySetup ToMasterySetup(JToken setup)
        {
            foreach (JToken item in setup["masteries"].AsEnumerable<JToken>())
            {
                item["rank"] = item["points"];
            }
            return setup.ToObject<MasterySetup>();
        }

        private static InventoryHelper.RuneSetup ToRuneSetup(JToken obj)
        {
            return obj.ToObject<InventoryHelper.RuneSetup>();
        }

        private static SpellBookPageDTO ToSpellBookPage(InventoryHelper.RuneSetup setup)
        {
            List<SlotEntry> slotEntries = new List<SlotEntry>();
            IGrouping<RuneType, InventoryHelper.Rune>[] array = (
                from x in (IEnumerable<InventoryHelper.Rune>)setup.Runes
                group x by InventoryHelper.GetRuneType(x.Id) into x
                orderby x.Key
                select x).ToArray<IGrouping<RuneType, InventoryHelper.Rune>>();
            for (int i = 0; i < (int)array.Length; i++)
            {
                IGrouping<RuneType, InventoryHelper.Rune> runeTypes = array[i];
                RuneType key = runeTypes.Key;
                InventoryHelper.Rune[] runeArray = runeTypes.ToArray<InventoryHelper.Rune>();
                int num = (byte)key * 9 + (byte)RuneType.Yellow;
                for (int j = 0; j < (int)runeArray.Length; j++)
                {
                    InventoryHelper.Rune rune = runeArray[j];
                    for (int k = 0; k < rune.Count; k++)
                    {
                        SlotEntry slotEntry = new SlotEntry()
                        {
                            RuneId = rune.Id,
                            RuneSlotId = num
                        };
                        slotEntries.Add(slotEntry);
                        num++;
                    }
                }
            }
            SpellBookPageDTO spellBookPageDTO = new SpellBookPageDTO()
            {
                PageId = setup.Id,
                Name = setup.Name,
                SlotEntries = slotEntries.ToList<SlotEntry>(),
                Current = true
            };
            return spellBookPageDTO;
        }

        public class Rune
        {
            public int Id;

            public int Count;

            public Rune()
            {
            }
        }

        public class RuneSetup
        {
            public double Id;

            public string Name;

            public InventoryHelper.Rune[] Runes;

            public RuneSetup()
            {
            }
        }
    }
}