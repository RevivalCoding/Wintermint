using Chat;
using System;

namespace WintermintClient.Riot
{
    public class UberChatClient : ChatClient
    {
        public RiotAccount Account;

        public int AccountHandle
        {
            get
            {
                return this.Account.Handle;
            }
        }

        public string RealmId
        {
            get
            {
                return this.Account.RealmId;
            }
        }

        public UberChatClient(RiotAccount account)
        {
            this.Account = account;
        }
    }
}