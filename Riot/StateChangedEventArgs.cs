using System;

namespace WintermintClient.Riot
{
    public class StateChangedEventArgs : EventArgs
    {
        public ConnectionState OldState;

        public ConnectionState NewState;

        public StateChangedEventArgs(ConnectionState oldState, ConnectionState newState)
        {
            this.OldState = oldState;
            this.NewState = newState;
        }
    }
}