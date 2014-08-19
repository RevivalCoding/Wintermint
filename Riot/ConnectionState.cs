using System;

namespace WintermintClient.Riot
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Waiting,
        Error
    }
}