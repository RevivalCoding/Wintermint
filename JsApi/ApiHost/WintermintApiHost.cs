using Browser.BrowserWindows;
using Browser.Rpc;
using Complete.Extensions;
using MicroApi;
using RiotGames.Platform.Messaging;
using RtmpSharp.Messaging;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Helpers;

namespace WintermintClient.JsApi.ApiHost
{
    public class WintermintApiHost
    {
        private readonly MicroApiHost api;

        private readonly IBrowserWindow browserWindow;

        public WintermintApiHost(IBrowserWindow browserWindow)
        {
            this.browserWindow = browserWindow;
            this.api = new MicroApiHost(InstantiationPolicy.Once);
            this.api.LoadServices(typeof(WintermintApiHost).Assembly);
        }

        private static string GetJsStyleClassName(object obj)
        {
            if (obj == null)
            {
                return "null";
            }
            return obj.GetType().Name.Dasherize();
        }

        public async void ProcessRequest(RequestContext request)
        {
            try
            {
                await this.ProcessRequestInternal(request);
            }
            catch (Exception exception)
            {
            }
        }

        public async Task ProcessRequestInternal(RequestContext request)
        {
            bool flag;
            object json;
            object[] objArray;
            object[] objArray1;
            object[] argument;
            try
            {
                request.ChangeCefBrowser(this.browserWindow.CefBrowser);
                JsApiService.JsResponse jsResponse = new JsApiService.JsResponse(request.OnProgress);
                JsApiService.JsResponse jsResponse1 = new JsApiService.JsResponse(request.OnResult);
                MethodData method = this.api.GetMethod(request.Method);
                ParameterInfo[] parameters = method.Parameters;
                int length = (int)method.Parameters.Length;
                flag = (length < 1 ? false : parameters[0].ParameterType == typeof(JsApiService.JsResponse));
                bool flag1 = flag;
                switch (length)
                {
                    case 0:
                        {
                            argument = new object[0];
                            break;
                        }
                    case 1:
                        {
                            objArray = (flag1 ? new object[] { jsResponse1 } : new object[] { request.Argument });
                            argument = objArray;
                            break;
                        }
                    case 2:
                        {
                            objArray1 = (flag1 ? new object[] { jsResponse, jsResponse1 } : new object[] { request.Argument, jsResponse1 });
                            argument = objArray1;
                            break;
                        }
                    case 3:
                        {
                            argument = new object[] { request.Argument, jsResponse, jsResponse1 };
                            break;
                        }
                    default:
                        {
                            throw new ArgumentException("Unknown number of parameters in microapi method.");
                        }
                }
                object obj = await this.api.InvokeAsync(request.Method, argument);
                object obj1 = obj;
                JsonResult jsonResult = obj1 as JsonResult;
                JsApiService.JsResponse jsResponse2 = jsResponse1;
                if (jsonResult != null)
                {
                    json = jsonResult.Json;
                }
                else
                {
                    json = obj1;
                }
                jsResponse2(json);
            }
            catch (MissingMethodException missingMethodException)
            {
                request.OnFault(new { Reason = "missing-method", Data = null });
            }
            catch (Exception exception)
            {
                Exception innerException = exception;
                TargetInvocationException targetInvocationException = innerException as TargetInvocationException;
                if (targetInvocationException != null)
                {
                    innerException = targetInvocationException.InnerException;
                }
                string reason = null;
                object info = null;
                JsApiException jsApiException = innerException as JsApiException;
                if (jsApiException != null)
                {
                    reason = jsApiException.Reason;
                    info = jsApiException.Info;
                }
                InvocationException invocationException = innerException as InvocationException;
                if (invocationException != null)
                {
                    PlatformException rootCause = invocationException.RootCause as PlatformException;
                    if (rootCause != null)
                    {
                        reason = rootCause.RootCauseClassname;
                    }
                }
                string jsStyleClassName = reason;
                if (jsStyleClassName == null)
                {
                    jsStyleClassName = WintermintApiHost.GetJsStyleClassName(innerException);
                }
                reason = jsStyleClassName;
                request.OnFault(new { Reason = reason, Data = info });
            }
            return;
            throw new ArgumentException("Unknown number of parameters in microapi method.");
        }
    }
}