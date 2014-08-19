using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using WintermintData.Riot.Account;

namespace WintermintClient.Riot
{
    internal class RiotAccountBag
    {
        private readonly HashSet<RiotAccount> accounts;

        public RiotAccount Active
        {
            get;
            private set;
        }

        public RiotAccountBag()
        {
            this.accounts = new HashSet<RiotAccount>();
        }

        private void AccountOnStateChanged(object sender, StateChangedEventArgs args)
        {
            RiotAccount riotAccount = sender as RiotAccount;
            if (riotAccount == null)
            {
                return;
            }
            if (args.NewState == ConnectionState.Disconnected && args.OldState != args.NewState)
            {
                riotAccount.ReconnectThrottledAsync();
            }
        }

        public RiotAccount Attach(AccountConfig config)
        {
            RiotAccount riotAccount = this.accounts.FirstOrDefault<RiotAccount>((RiotAccount x) =>
            {
                if (x.Username != config.Username)
                {
                    return false;
                }
                return x.RealmId == config.RealmId;
            });
            if (riotAccount != null)
            {
                return riotAccount;
            }
            RiotAccount riotAccount1 = new RiotAccount(config)
            {
                CanConnect = true
            };
            riotAccount1.ConnectAsync();
            this.accounts.Add(riotAccount1);
            this.OnAccountAdded(riotAccount1);
            return riotAccount1;
        }

        public void Detach(RiotAccount account)
        {
            if (!this.accounts.Contains(account))
            {
                return;
            }
            if (account == this.Active)
            {
                this.SetActive(this.accounts.FirstOrDefault<RiotAccount>((RiotAccount x) =>
                {
                    if (x == account)
                    {
                        return false;
                    }
                    return x.RealmId == account.RealmId;
                }) ?? this.accounts.FirstOrDefault<RiotAccount>());
            }
            account.CanConnect = false;
            this.accounts.Remove(account);
            account.Close();
            this.OnAccountRemoved(account);
        }

        public void FireActiveChanged(RiotAccount account)
        {
            if (this.ActiveChanged != null)
            {
                this.ActiveChanged(this, account);
            }
        }

        public RiotAccount Get(int handle)
        {
            return this.accounts.FirstOrDefault<RiotAccount>((RiotAccount x) => x.Handle == handle);
        }

        public RiotAccount Get(string realm)
        {
            return this.Get(realm, RiotAccountPreference.LeastContention);
        }

        public RiotAccount Get(string realm, RiotAccountPreference preference)
        {
            switch (preference)
            {
                case RiotAccountPreference.Active:
                    {
                        if (this.Active == null)
                        {
                            return null;
                        }
                        if (this.Active.RealmId != realm)
                        {
                            return null;
                        }
                        return this.Active;
                    }
                case RiotAccountPreference.Inactive:
                case RiotAccountPreference.LeastContention:
                    {
                        IEnumerable<RiotAccount> active = this.accounts.Where<RiotAccount>((RiotAccount x) =>
                        {
                            if (x.RealmId != realm)
                            {
                                return false;
                            }
                            return x.State == ConnectionState.Connected;
                        });
                        if (preference == RiotAccountPreference.Inactive)
                        {
                            active =
                                from x in active
                                where x != this.Active
                                select x;
                        }
                        RiotAccount[] array = active.ToArray<RiotAccount>();
                        if ((int)array.Length <= 0)
                        {
                            return null;
                        }
                        return ((IEnumerable<RiotAccount>)array).MinBy<RiotAccount, int>((RiotAccount x) => x.PendingInvocations);
                    }
                case RiotAccountPreference.InactivePreferred:
                    {
                        return this.Get(realm, RiotAccountPreference.Inactive) ?? this.Get(realm, RiotAccountPreference.Active);
                    }
            }
            throw new ArgumentOutOfRangeException("preference");
        }

        public RiotAccount[] GetAll()
        {
            return this.accounts.ToArray<RiotAccount>();
        }

        public void OnAccountAdded(RiotAccount account)
        {
            if (this.AccountAdded != null)
            {
                account.StateChanged += new EventHandler<StateChangedEventArgs>(this.AccountOnStateChanged);
                this.AccountAdded(this, account);
            }
        }

        public void OnAccountRemoved(RiotAccount account)
        {
            if (this.AccountRemoved != null)
            {
                account.StateChanged -= new EventHandler<StateChangedEventArgs>(this.AccountOnStateChanged);
                this.AccountRemoved(this, account);
            }
        }

        public void SetActive(RiotAccount account)
        {
            if (account != null && !this.accounts.Contains(account))
            {
                throw new AccountNotFoundException("The specified account is not registered.");
            }
            this.Active = account;
            this.FireActiveChanged(account);
        }

        public event EventHandler<RiotAccount> AccountAdded;

        public event EventHandler<RiotAccount> AccountRemoved;

        public event EventHandler<RiotAccount> ActiveChanged;
    }
}