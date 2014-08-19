using Chat;
using Complete;
using Complete.Extensions;
using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using RiotGames.Platform.Game;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using WintermintClient.Data;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("chat.status", Preload = true)]
    public class ChatStatusService : JsApiService
    {
        private const string StatusXmlFormat = "<body>  <profileIcon>532</profileIcon>  <level>30</level>  <wins>1</wins>  <leaves>0</leaves>  <odinWins>0</odinWins>  <odinLeaves>0</odinLeaves>  <queueType />  <rankedLosses>0</rankedLosses>  <rankedRating>0</rankedRating>  <tier>DIAMOND</tier>  <statusMsg></statusMsg>  <timeStamp>{0}</timeStamp>  <rankedLeagueName>Wintermint Dreamyland</rankedLeagueName>  <rankedLeagueDivision>I</rankedLeagueDivision>  <rankedLeagueTier>DIAMOND</rankedLeagueTier>  <rankedLeagueQueue>RANKED_SOLO_5x5</rankedLeagueQueue>  <isObservable>ALL</isObservable>  <gameQueueType>{1}</gameQueueType>  <skinname>{2}</skinname>  <gameStatus>{3}</gameStatus></body>";

        private string timestamp;

        private RiotAccount __account;

        private string __type;

        private DateTime __timestamp;

        private static string InactiveXmlStatus;

        static ChatStatusService()
        {
            object[] empty = new object[] { string.Empty, string.Empty, string.Empty, "outOfGame" };
            ChatStatusService.InactiveXmlStatus = string.Format("<body>  <profileIcon>532</profileIcon>  <level>30</level>  <wins>1</wins>  <leaves>0</leaves>  <odinWins>0</odinWins>  <odinLeaves>0</odinLeaves>  <queueType />  <rankedLosses>0</rankedLosses>  <rankedRating>0</rankedRating>  <tier>DIAMOND</tier>  <statusMsg></statusMsg>  <timeStamp>{0}</timeStamp>  <rankedLeagueName>Wintermint Dreamyland</rankedLeagueName>  <rankedLeagueDivision>I</rankedLeagueDivision>  <rankedLeagueTier>DIAMOND</rankedLeagueTier>  <rankedLeagueQueue>RANKED_SOLO_5x5</rankedLeagueQueue>  <isObservable>ALL</isObservable>  <gameQueueType>{1}</gameQueueType>  <skinname>{2}</skinname>  <gameStatus>{3}</gameStatus></body>", empty);
        }

        public ChatStatusService()
        {
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                ChatClient.__Presence presence = account.Chat.Presence;
                presence.Message = ChatStatusService.InactiveXmlStatus;
                presence.Status = PresenceType.Online;
            });
        }

        [MicroApiMethod("set")]
        public void Chat(dynamic args)
        {
            int num = (int)args.accountHandle;
            string str = (string)args.type;
            RiotAccount riotAccount = JsApiService.AccountBag.Get(num);
            string timestamp = this.GetTimestamp(riotAccount, str);
            if (this.timestamp == timestamp)
            {
                return;
            }
            this.timestamp = timestamp;
            string xmlStatus = this.GetXmlStatus(riotAccount, timestamp, str);
            try
            {
                ChatClient.__Presence presence = riotAccount.Chat.Presence;
                presence.Message = xmlStatus;
                presence.Status = ChatStatusService.GetStatusForType(str);
                presence.Post();
            }
            catch
            {
            }
            foreach (RiotAccount riotAccount1 in
                from x in JsApiService.AccountBag.GetAll()
                where x != riotAccount
                select x)
            {
                try
                {
                    ChatClient.__Presence inactiveXmlStatus = riotAccount1.Chat.Presence;
                    inactiveXmlStatus.Message = ChatStatusService.InactiveXmlStatus;
                    inactiveXmlStatus.Status = PresenceType.Online;
                    inactiveXmlStatus.Post();
                }
                catch
                {
                }
            }
        }

        private static PresenceType GetStatusForType(string type)
        {
            string str = type;
            string str1 = str;
            if (str != null && (str1 == "in-game" || str1 == "in-queue"))
            {
                return PresenceType.Busy;
            }
            return PresenceType.Online;
        }

        private string GetTimestamp(RiotAccount account, string type)
        {
            if (account != this.__account || type != this.__type)
            {
                this.__timestamp = DateTime.UtcNow;
            }
            this.__account = account;
            this.__type = type;
            TimeSpan _Timestamp = this.__timestamp - UnixDateTime.Epoch;
            return ((long)_Timestamp.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);
        }

        private string GetXmlStatus(RiotAccount account, string timestamp, string type)
        {
            GameDTO game = account.Game ?? new GameDTO();
            List<PlayerChampionSelectionDTO> playerChampionSelections = game.PlayerChampionSelections ?? new List<PlayerChampionSelectionDTO>();
            PlayerChampionSelectionDTO playerChampionSelectionDTO = playerChampionSelections.FirstOrDefault<PlayerChampionSelectionDTO>((PlayerChampionSelectionDTO x) => x.SummonerInternalName == account.SummonerInternalName) ?? new PlayerChampionSelectionDTO();
            object[] legacyChampionClientNameOrSoraka = new object[] { timestamp, null, null, null };
            legacyChampionClientNameOrSoraka[1] = (string.IsNullOrEmpty(game.QueueTypeName) ? "NONE" : game.QueueTypeName);
            legacyChampionClientNameOrSoraka[2] = ChampionNameData.GetLegacyChampionClientNameOrSoraka(playerChampionSelectionDTO.ChampionId);
            legacyChampionClientNameOrSoraka[3] = type.Camelize();
            return string.Format("<body>  <profileIcon>532</profileIcon>  <level>30</level>  <wins>1</wins>  <leaves>0</leaves>  <odinWins>0</odinWins>  <odinLeaves>0</odinLeaves>  <queueType />  <rankedLosses>0</rankedLosses>  <rankedRating>0</rankedRating>  <tier>DIAMOND</tier>  <statusMsg></statusMsg>  <timeStamp>{0}</timeStamp>  <rankedLeagueName>Wintermint Dreamyland</rankedLeagueName>  <rankedLeagueDivision>I</rankedLeagueDivision>  <rankedLeagueTier>DIAMOND</rankedLeagueTier>  <rankedLeagueQueue>RANKED_SOLO_5x5</rankedLeagueQueue>  <isObservable>ALL</isObservable>  <gameQueueType>{1}</gameQueueType>  <skinname>{2}</skinname>  <gameStatus>{3}</gameStatus></body>", legacyChampionClientNameOrSoraka);
        }
    }
}