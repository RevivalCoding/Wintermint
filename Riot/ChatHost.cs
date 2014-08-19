using Chat;
using System;
using System.Collections.Generic;
using WintermintData.Riot.Account;

namespace WintermintClient.Riot
{
    public class ChatHost
    {
        public ChatHost()
        {
        }

        public static UberChatClient Create(AccountConfig config, RiotAccount account)
        {
            Uri uri = new Uri(config.Endpoints.Chat.Uri);
            UberChatClient uberChatClient = new UberChatClient(account)
            {
                Host = uri.Host,
                Port = uri.Port,
                Server = "pvp.net",
                Username = config.Username,
                Password = string.Concat("AIR_", config.Password)
            };
            UberChatClient uberChatClient1 = uberChatClient;
            uberChatClient1.ConferenceServers.AddRange(config.Endpoints.Chat.Conference);
            return uberChatClient1;
        }
    }
}