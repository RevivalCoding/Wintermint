using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WintermintClient;
using WintermintClient.Data;
using WintermintClient.JsApi;
using WintermintClient.JsApi.Helpers;
using WintermintData.Account;
using WintermintData.Protocol;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("wintermint", Preload = true)]
    public class WintermintService : JsApiService
    {
        private string email;

        private string password;

        public WintermintService()
        {
            JsApiService.Client.Disconnected += new EventHandler(this.OnClientDisconnected);
        }

        [MicroApiMethod("edit.password")]
        public async Task ChangePassword(dynamic args)
        {
            try
            {
                string str = (string)args.oldPassword;
                string str1 = (string)args.newPassword;
                LittleClient client = JsApiService.Client;
                object[] objArray = new object[] { str, str1 };
                await client.Invoke("account.edit.password", objArray);
                this.password = str1;
            }
            catch (ProtocolVersionNegotiationException protocolVersionNegotiationException)
            {
                if (protocolVersionNegotiationException.IsOld)
                {
                    WintermintService.InitiateCaptiveUpdate();
                }
                throw new JsApiException("protocol-mismatch");
            }
            catch (Exception exception1)
            {
                Exception exception = WintermintService.TransformAccountException(exception1);
                if (exception == null)
                {
                    throw new JsApiException("unknown");
                }
                throw exception;
            }
        }

        private static void InitiateCaptiveUpdate()
        {
            foreach (Process process in ((IEnumerable<Process>)Process.GetProcesses()).Where<Process>((Process x) =>
            {
                if (x.ProcessName == "wintermint-update")
                {
                    return true;
                }
                return x.ProcessName == "wintermint-update-ui";
            }))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
            }
            if (LaunchData.TryLaunch("wintermint-update-ui", "role:captive-update"))
            {
                Environment.Exit(0);
            }
            MessageBox.Show("Wintermint needs to be updated because it's too old to talk with the server.\n\nHowever, the updater could not be started. Please update Wintermint manually.", "Wintermint Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        [MicroApiMethod("login")]
        public async Task Login(dynamic args)
        {
            try
            {
                this.email = (string)args.email;
                this.password = (string)args.password;
                await JsApiService.Client.AuthenticateAsync(this.email, this.password);
                JsApiService.Push("auth:success", JsApiService.Client.Account);
            }
            catch (Exception exception1)
            {
                Exception exception = WintermintService.TransformAccountException(exception1);
                if (exception == null)
                {
                    throw new JsApiException("unknown");
                }
                throw exception;
            }
        }

        private async void OnClientDisconnected(object sender, EventArgs e)
        {
            JsApiService.Push("auth:disconnected", null);
            try
            {
                await JsApiService.Client.AuthenticateAsync(this.email, this.password);
                JsApiService.Push("auth:success", JsApiService.Client.Account);
                return;
            }
            catch (ProtocolVersionNegotiationException protocolVersionNegotiationException)
            {
                if (protocolVersionNegotiationException.IsOld)
                {
                    WintermintService.InitiateCaptiveUpdate();
                }
                JsApiService.Push("auth:fail", new JsApiException("protocol-mismatch"));
            }
            catch (Exception exception1)
            {
                Exception exception = WintermintService.TransformAccountException(exception1);
                if (exception != null)
                {
                    JsApiService.Push("auth:fail", exception);
                }
            }
            await Task.Delay(5000);
            ThreadPool.QueueUserWorkItem((object x) => this.OnClientDisconnected(sender, e));
        }

        private static Exception TransformAccountException(Exception exception)
        {
            AccountException accountException = exception as AccountException;
            if (accountException == null)
            {
                if (!(exception is ProtocolVersionNegotiationException))
                {
                    return null;
                }
                return new JsApiException("protocol-mismatch");
            }
            if (accountException is AccountNotFoundException)
            {
                return new JsApiException("account-not-found");
            }
            if (accountException is AccountNotUniqueException)
            {
                return new JsApiException("account-not-unique");
            }
            if (accountException is InvalidCredentialsException)
            {
                return new JsApiException("invalid-credentials");
            }
            if (accountException is CredentialStrengthException)
            {
                return new JsApiException("credential-strength");
            }
            HistoricalCredentialsException historicalCredentialsException = accountException as HistoricalCredentialsException;
            if (historicalCredentialsException == null)
            {
                return accountException;
            }
            return new JsApiException("historial-credentials", new { LastValid = historicalCredentialsException.LastValid });
        }
    }
}