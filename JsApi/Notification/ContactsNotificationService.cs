using Chat;
using MicroApi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Notification
{
    [MicroApiService("contacts", Preload = true)]
    public class ContactsNotificationService : JsApiService
    {
        private readonly static Presence OfflinePresence;

        private readonly static ConcurrentDictionary<string, ContactsNotificationService.JsFederatedDude> Contacts;

        static ContactsNotificationService()
        {
            ContactsNotificationService.OfflinePresence = new Presence()
            {
                PresenceType = PresenceType.Offline
            };
            ContactsNotificationService.Contacts = new ConcurrentDictionary<string, ContactsNotificationService.JsFederatedDude>();
        }

        public ContactsNotificationService()
        {
            JsApiService.AccountBag.AccountAdded += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                UberChatClient chat = account.Chat;
                chat.ContactChanged += new EventHandler<ContactChangedEventArgs>(this.OnContactChanged);
                chat.RosterReceived += new EventHandler<RosterReceivedEventArgs>(this.ChatOnRosterReceived);
                chat.MessageReceived += new EventHandler<MessageReceivedEventArgs>(this.OnMessageReceived);
                chat.MailReceived += new EventHandler<MessageReceivedEventArgs>(this.OnMailReceived);
                chat.ErrorReceived += new EventHandler<ErrorReceivedEventArgs>(this.OnErrorReceived);
            });
            JsApiService.AccountBag.AccountRemoved += new EventHandler<RiotAccount>((object sender, RiotAccount account) =>
            {
                UberChatClient chat = account.Chat;
                chat.ContactChanged -= new EventHandler<ContactChangedEventArgs>(this.OnContactChanged);
                chat.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(this.OnMessageReceived);
                chat.MailReceived -= new EventHandler<MessageReceivedEventArgs>(this.OnMailReceived);
                chat.ErrorReceived -= new EventHandler<ErrorReceivedEventArgs>(this.OnErrorReceived);
                foreach (Contact roster in chat.Roster)
                {
                    this.OnContactChanged(chat, new ContactChangedEventArgs(roster, ContactChangeType.Remove));
                }
            });
        }

        private static void ApplyContactTransformations(ContactsNotificationService.JsContact contact)
        {
            if (contact.Presences != null)
            {
                foreach (Presence presence1 in
                    from presence in (IEnumerable<Presence>)contact.Presences
                    where presence.Resource == "xiff"
                    select presence)
                {
                    presence1.Resource = "adobe air";
                }
            }
            string[] groups = contact.Groups;
            if (groups != null)
            {
                for (int i = 0; i < (int)groups.Length; i++)
                {
                    if (groups[i] == "**Default")
                    {
                        groups[i] = "General";
                    }
                }
                contact.Groups = groups.Distinct<string>(StringComparer.OrdinalIgnoreCase).ToArray<string>();
            }
        }

        private async void ChatOnRosterReceived(object sender, RosterReceivedEventArgs args)
        {
            try
            {
                UberChatClient uberChatClient = (UberChatClient)sender;
                RiotAccount account = uberChatClient.Account;
                Contact[] contacts = args.Contacts;
                Contact[] contactArray = contacts;
                long[] array = (
                    from c in (IEnumerable<Contact>)contactArray
                    select JsApiService.GetSummonerIdFromJid(c.Jid)).ToArray<long>();
                RiotAccount riotAccount = account;
                object[] objArray = new object[] { array };
                string[] strArrays = await riotAccount.InvokeAsync<string[]>("summonerService", "getSummonerNames", objArray);
                for (int i = 0; i < (int)contacts.Length; i++)
                {
                    Contact contact = contacts[i];
                    string str = strArrays[i];
                    if (contact.Name != str)
                    {
                        contact.Name = str;
                        this.OnContactUpdated(uberChatClient, ContactsNotificationService.JsContact.Create(uberChatClient, contact));
                        uberChatClient.Contacts.Update(contact.BareJid, contact.Name, contact.Groups);
                    }
                }
            }
            catch (Exception exception)
            {
            }
        }

        private static int GetPresenceOrder(Presence presence)
        {
            switch (presence.PresenceType)
            {
                case PresenceType.Offline:
                    {
                        return 3;
                    }
                case PresenceType.Online:
                    {
                        return 0;
                    }
                case PresenceType.Busy:
                    {
                        return 1;
                    }
                case PresenceType.Away:
                    {
                        return 2;
                    }
            }
            return 10;
        }

        private void NotifyXmppMessage(string publishKey, object sender, MessageReceivedEventArgs args)
        {
            if (args.MessageId != null && args.MessageId.StartsWith("hm_", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (args.Sender.ConferenceUser && string.IsNullOrEmpty(args.Sender.Name))
            {
                return;
            }
            UberChatClient uberChatClient = sender as UberChatClient;
            ContactsNotificationService.JsContact nums = ContactsNotificationService.JsContact.Create(uberChatClient, args.Sender);
            int[] accountHandle = new int[] { uberChatClient.AccountHandle };
            nums.Accounts = new List<int>(accountHandle);
            var variable = new { Sender = nums, Subject = args.Subject, Message = args.Message, Timestamp = args.Timestamp, RealmId = uberChatClient.RealmId, Handle = uberChatClient.AccountHandle };
            JsApiService.Push(publishKey, variable);
        }

        private void OnContactChanged(object sender, ContactChangedEventArgs args)
        {
            UberChatClient uberChatClient = (UberChatClient)sender;
            ContactsNotificationService.JsContact jsContact = ContactsNotificationService.JsContact.Create(uberChatClient, args.Contact);
            ContactsNotificationService.ApplyContactTransformations(jsContact);
            switch (args.ChangeType)
            {
                case ContactChangeType.Add:
                    {
                        return;
                    }
                case ContactChangeType.Update:
                    {
                        this.OnContactUpdated(uberChatClient, jsContact);
                        return;
                    }
                case ContactChangeType.Remove:
                    {
                        this.OnContactRemoved(uberChatClient, jsContact);
                        return;
                    }
            }
            throw new ArgumentOutOfRangeException();
        }

        private void OnContactRemoved(UberChatClient client, ContactsNotificationService.JsContact contact)
        {
            ContactsNotificationService.JsFederatedDude jsFederatedDude;
            lock (ContactsNotificationService.Contacts)
            {
                if (ContactsNotificationService.Contacts.TryGetValue(contact.InternalId, out jsFederatedDude) && jsFederatedDude.Detach(client.AccountHandle) == 0)
                {
                    ContactsNotificationService.Contacts.TryRemove(contact.InternalId, out jsFederatedDude);
                }
            }
            if (jsFederatedDude != null)
            {
                ContactsNotificationService.PushContact((jsFederatedDude.AccountHandles.Count > 0 ? "chat:contact:upsert" : "chat:contact:delete"), jsFederatedDude);
            }
        }

        private void OnContactUpdated(UberChatClient client, ContactsNotificationService.JsContact contact)
        {
            ContactsNotificationService.JsFederatedDude orAdd;
            lock (ContactsNotificationService.Contacts)
            {
                orAdd = ContactsNotificationService.Contacts.GetOrAdd(contact.InternalId, (string _) => new ContactsNotificationService.JsFederatedDude(contact));
                orAdd.Contact = contact;
                orAdd.Attach(client.AccountHandle);
            }
            ContactsNotificationService.PushContact("chat:contact:upsert", orAdd);
        }

        private void OnErrorReceived(object sender, ErrorReceivedEventArgs errorReceivedEventArgs)
        {
        }

        private void OnMailReceived(object sender, MessageReceivedEventArgs args)
        {
            this.NotifyXmppMessage("chat:mail", sender, args);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            this.NotifyXmppMessage("chat:message", sender, args);
        }

        private static void PushContact(string key, ContactsNotificationService.JsFederatedDude dude)
        {
            ContactsNotificationService.JsContact contact = dude.Contact;
            if (contact != null)
            {
                contact.Accounts = dude.AccountHandles;
            }
            JsApiService.Push(key, contact);
        }

        [MicroApiMethod("refresh")]
        public void RefreshContacts()
        {
            JsApiService.Push("chat:contact:wipe", null);
            foreach (ContactsNotificationService.JsFederatedDude value in ContactsNotificationService.Contacts.Values)
            {
                ContactsNotificationService.PushContact("chat:contact:upsert", value);
            }
        }

        [Flags]
        public enum ContactUpdateType
        {
            Upsert,
            Delete
        }

        [Serializable]
        public class JsContact
        {
            public string InternalId;

            public string RealmId;

            public Presence Presence;

            public List<int> Accounts;

            public string Id;

            public string Jid;

            public string Name;

            public string[] Groups;

            public Presence[] Presences;

            public bool ConferenceUser;

            public string BareJid;

            public JsContact()
            {
            }

            public static ContactsNotificationService.JsContact Create(UberChatClient client, Contact contact)
            {
                ContactsNotificationService.JsContact jsContact = new ContactsNotificationService.JsContact()
                {
                    InternalId = string.Format("{0}//{1}", client.RealmId, contact.Id),
                    RealmId = client.RealmId,
                    Presence = contact.Presences.OrderBy<Presence, int>(new Func<Presence, int>(ContactsNotificationService.GetPresenceOrder)).FirstOrDefault<Presence>() ?? ContactsNotificationService.OfflinePresence,
                    Id = contact.Id,
                    Jid = contact.Jid,
                    Name = contact.Name,
                    Groups = contact.Groups,
                    Presences = contact.Presences,
                    ConferenceUser = contact.ConferenceUser,
                    BareJid = contact.BareJid
                };
                return jsContact;
            }
        }

        public class JsFederatedDude
        {
            public ContactsNotificationService.JsContact Contact;

            public List<int> AccountHandles;

            public JsFederatedDude(ContactsNotificationService.JsContact contact)
            {
                this.Contact = contact;
                this.AccountHandles = new List<int>(1);
            }

            public void Attach(int accountHandle)
            {
                if (this.AccountHandles.Contains(accountHandle))
                {
                    return;
                }
                this.AccountHandles.Add(accountHandle);
            }

            public int Detach(int accountHandle)
            {
                this.AccountHandles.Remove(accountHandle);
                return this.AccountHandles.Count;
            }
        }
    }
}