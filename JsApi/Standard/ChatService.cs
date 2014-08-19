using Chat;
using MicroApi;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using WintermintClient.JsApi;
using WintermintClient.Riot;

namespace WintermintClient.JsApi.Standard
{
    [MicroApiService("chat")]
    public class ChatService : JsApiService
    {
        public ChatService()
        {
        }

        [MicroApiMethod("chat")]
        public void Chat(dynamic args)
        {
            ChatClient chatClient = (ChatClient)this.GetChatClient(args);
            ChatClient.__Chat chat = chatClient.Chat;
            string str = (string)args.jid;
            string str1 = (string)args.message;
            if (chatClient.ConferenceServers.Any<string>((string x) => (new JabberId(str)).Server == x))
            {
                chat.GroupChat(str, str1);
                return;
            }
            chat.Chat(str, str1);
        }

        private static ChatClient GetChatClient(dynamic args)
        {
            int num = (int)args.accountHandle;
            return JsApiService.AccountBag.Get(num).Chat;
        }

        [MicroApiMethod("join")]
        public void JoinRoom(dynamic args)
        {
            dynamic obj = this.GetChatClient(args);
            string str = (string)args.jid;
            string str1 = (string)args.password;
            obj.Muc.Join(str, str1);
        }

        [MicroApiMethod("leave")]
        public void LeaveRoom(dynamic args)
        {
            dynamic obj = this.GetChatClient(args);
            string str = (string)args.jid;
            obj.Muc.Leave(str);
        }

        [MicroApiMethod("send")]
        public void Message(dynamic args)
        {
            dynamic obj = this.GetChatClient(args);
            string str = (string)args.jid;
            string str1 = (string)args.subject;
            string str2 = (string)args.message;
            obj.Chat.Message(str, str1, str2);
        }
    }
}