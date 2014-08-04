using System;
using System.Collections.Generic;
using XSockets.ClientIOS.Common.Event.Arguments;

namespace XSockets.ClientIOS.Common.Interfaces
{
    public interface IXSocketController
    {
        int BufferSize { get; set; }
        string Alias { get; set; }
        bool CorrectController { get; set; }
        bool IsChannel { get; set; }
        IXSocketJsonSerializer JsonSerializer { get; set; }        
        Guid ClientGuid { get; set; }
        Guid StorageGuid { get; set; }

        event EventHandler<OnClientConnectArgs> OnOpen;
        event EventHandler<OnClientDisconnectArgs> OnClose;
        event EventHandler<OnErrorArgs> OnError;
        
        IXSocketController NewInstance();

        void OnMessage(IMessage message);

        bool Available();

        void Close();
        void Open();
        
        void Send(byte[] data);

        bool SubscribesTo(string @event);

        IList<string> Subscriptions { get; set; }
    }
}