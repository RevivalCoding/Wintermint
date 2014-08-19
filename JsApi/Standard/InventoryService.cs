using MicroApi;
using RiotGames.Platform.Catalog.Champion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WintermintClient;
using WintermintClient.Data;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Helpers;
using WintermintClient.Riot;
using WintermintData.Storage;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("inventory")]
    public class InventoryService : JsApiService
    {
        private static ChampionGroup[] NoChampionGroups;

        static InventoryService()
        {
            InventoryService.NoChampionGroups = new ChampionGroup[0];
        }

        public InventoryService()
        {
        }

        private async Task<InventoryService.JsChampion[]> GetAllChampions()
        {
            bool flag;
            InventoryService.JsChampion[] jsChampionArray;
            bool flag1;
            RiotAccount riotAccount = JsApiService.RiotAccount;
            InventoryService.JsChampion[] jsChampionArray1 = JsApiService.Cache.Get<InventoryService.JsChampion[]>("riot:champions");
            long? nullable = JsApiService.Cache.Get<long?>("riot:champions:handle");
            if (jsChampionArray1 != null && nullable.HasValue)
            {
                long? nullable1 = nullable;
                long handle = (long)riotAccount.Handle;
                flag1 = (nullable1.GetValueOrDefault() != handle ? false : nullable1.HasValue);
                if (flag1)
                {
                    jsChampionArray = jsChampionArray1;
                    return jsChampionArray;
                }
            }
            ChampionDTO[] championDTOArray = await riotAccount.InvokeAsync<ChampionDTO[]>("inventoryService", "getAvailableChampions");
            ChampionDTO[] championDTOArray1 = championDTOArray;
            for (int i = 0; i < (int)championDTOArray1.Length; i++)
            {
                ChampionDTO championDTO = championDTOArray1[i];
                ChampionDTO list = championDTO;
                List<ChampionSkinDTO> championSkins = championDTO.ChampionSkins;
                list.ChampionSkins = (
                    from x in championSkins
                    orderby x.SkinId
                    select x).ToList<ChampionSkinDTO>();
                List<ChampionSkinDTO> championSkinDTOs = championDTO.ChampionSkins;
                ChampionSkinDTO championSkinDTO = new ChampionSkinDTO()
                {
                    ChampionId = (double)championDTO.ChampionId,
                    Owned = true,
                    SkinId = championSkinDTOs[0].SkinId - 1
                };
                ChampionSkinDTO championSkinDTO1 = championSkinDTO;
                championSkinDTOs.Insert(0, championSkinDTO1);
                List<ChampionSkinDTO> championSkinDTOs1 = championSkinDTOs;
                if (!championSkinDTOs1.Any<ChampionSkinDTO>((ChampionSkinDTO x) => x.LastSelected))
                {
                    championSkinDTO1.LastSelected = true;
                }
            }
            InventoryService.JsChampion[] array = championDTOArray.Select<ChampionDTO, InventoryService.JsChampion>(new Func<ChampionDTO, InventoryService.JsChampion>(InventoryService.ToJsChampion)).ToArray<InventoryService.JsChampion>();
            JsApiService.Cache.Set("riot:champions", array);
            JsApiService.Cache.Set("riot:champions:handle", new long?((long)riotAccount.Handle));
            jsChampionArray = array;
            return jsChampionArray;
        }

        [MicroApiMethod("getChampions")]
        public async Task<object> GetChampions()
        {
            return await this.GetAllChampions();
        }

        [MicroApiMethod("getChampionGroups")]
        public async Task<object> GetChampionsGroups()
        {
            Task<ChampionGroupPreferences> task = JsApiService.Client.Invoke<ChampionGroupPreferences>("storage.get", "champion-groups");
            Task<InventoryService.JsChampion[]> allChampions = this.GetAllChampions();
            ChampionGroup[] localChampionGroups = this.GetLocalChampionGroups();
            InventoryService.JsChampion[] jsChampionArray = await allChampions;
            IEnumerable<ChampionGroup> championGroups = await task.Groups.Concat<ChampionGroup>(localChampionGroups);
            IEnumerable<InventoryService.JsChampionGroup> jsChampionGroup = 
                from group in championGroups
                select new InventoryService.JsChampionGroup()
                {
                    Name = group.Name,
                    Champions = jsChampionArray.Where<InventoryService.JsChampion>((InventoryService.JsChampion champion) => return group.Champions.Any<int>((int x) => x == champion.Id)).ToArray<InventoryService.JsChampion>()
                };
            InventoryService.JsChampionGroup[] jsChampionGroupArray = new InventoryService.JsChampionGroup[1];
            InventoryService.JsChampionGroup jsChampionGroup1 = new InventoryService.JsChampionGroup()
            {
                Name = "Available",
                Champions = jsChampionArray
            };
            jsChampionGroupArray[0] = jsChampionGroup1;
            return jsChampionGroup.Concat<InventoryService.JsChampionGroup>(jsChampionGroupArray).ToArray<InventoryService.JsChampionGroup>();
        }

        private ChampionGroup[] GetLocalChampionGroups()
        {
            ChampionGroup[] noChampionGroups;
            try
            {
                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string str = Path.Combine(folderPath, "wintermint", "experimental", "champion-groups");
                DirectoryInfo directoryInfo = new DirectoryInfo(str);
                noChampionGroups = (directoryInfo.Exists ? ((IEnumerable<FileInfo>)directoryInfo.GetFiles("*.txt")).Select<FileInfo, ChampionGroup>((FileInfo file) =>
                {
                    Dictionary<string, int> nameToId = ChampionNameData.NameToId;
                    IEnumerable<string> strs =
                        from x in File.ReadAllLines(file.FullName, Encoding.UTF8)
                        select x.Trim();
                    return new ChampionGroup()
                    {
                        Name = Path.GetFileNameWithoutExtension(file.Name),
                        Champions = strs.Where<string>(new Func<string, bool>(nameToId.ContainsKey)).Select<string, int>((string x) => nameToId[x]).ToArray<int>()
                    };
                }).ToArray<ChampionGroup>() : InventoryService.NoChampionGroups);
            }
            catch (Exception exception)
            {
                noChampionGroups = InventoryService.NoChampionGroups;
            }
            return noChampionGroups;
        }

        [MicroApiMethod("getMasteries")]
        public Task<object> GetMasteries()
        {
            RiotAccount active = JsApiService.AccountBag.Active;
            return InventoryHelper.GetMasterySetups(active.RealmId, (double)active.SummonerId);
        }

        [MicroApiMethod("getRunes")]
        public Task<object> GetRunes()
        {
            RiotAccount active = JsApiService.AccountBag.Active;
            return InventoryHelper.GetRuneSetups(active.RealmId, (double)active.SummonerId);
        }

        private static InventoryService.JsChampion ToJsChampion(ChampionDTO champion)
        {
            InventoryService.JsChampion jsChampion = new InventoryService.JsChampion()
            {
                Id = champion.ChampionId,
                Owned = champion.Owned,
                Free = champion.FreeToPlay,
                Skins = (
                    from s in champion.ChampionSkins
                    select new InventoryService.JsChampionSkin()
                    {
                        Id = (int)s.SkinId,
                        Owned = s.Owned
                    }).ToArray<InventoryService.JsChampionSkin>()
            };
            return jsChampion;
        }

        [Serializable]
        private class JsChampion
        {
            public int Id;

            public bool Owned;

            public bool Free;

            public InventoryService.JsChampionSkin[] Skins;

            public JsChampion()
            {
            }
        }

        [Serializable]
        private class JsChampionGroup
        {
            public string Name;

            public InventoryService.JsChampion[] Champions;

            public JsChampionGroup()
            {
            }
        }

        [Serializable]
        private class JsChampionSkin
        {
            public int Id;

            public bool Owned;

            public JsChampionSkin()
            {
            }
        }
    }
}