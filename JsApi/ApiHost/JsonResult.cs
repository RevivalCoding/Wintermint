using System;

namespace WintermintClient.JsApi.ApiHost
{
    [Serializable]
    public class JsonResult
    {
        public string Json;

        public JsonResult(string json)
        {
            this.Json = json;
        }
    }
}