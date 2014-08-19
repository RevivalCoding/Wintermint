using System;

namespace WintermintClient.Riot
{
    public class InvocationResultEventArgs : EventArgs
    {
        public string Service;

        public string Method;

        public bool Success;

        public object Result;

        public InvocationResultEventArgs(string service, string method, bool success, object result)
        {
            this.Service = service;
            this.Method = method;
            this.Success = success;
            this.Result = result;
        }
    }
}