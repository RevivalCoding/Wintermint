using System;

namespace WintermintClient.JsApi.Helpers
{
    [Serializable]
    public class JsApiException : Exception
    {
        public readonly string Reason;

        public readonly object Info;

        public JsApiException(string reason)
        {
            this.Reason = reason;
        }

        public JsApiException(string className, object info)
        {
            this.Reason = className;
            this.Info = info;
        }
    }
}