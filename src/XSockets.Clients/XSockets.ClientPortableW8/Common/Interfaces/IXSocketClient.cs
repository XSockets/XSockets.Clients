using System;
using System.Net;
using XSockets.ClientPortableW8.Common.Event.Arguments;
using XSockets.ClientPortableW8.Helpers;
using XSockets.ClientPortableW8.Model;
using XSockets.ClientPortableW8.Protocol;
using XSockets.ClientPortableW8.Utility.Storage;
using XSockets.ClientPortableW8.Wrapper;

namespace XSockets.ClientPortableW8.Common.Interfaces
{
    public interface IXSocketClient
    {
        string Origin { get; set; }
        bool AutoReconnect { get; set; }
        IXSocketJsonSerializer Serializer { get; set; }
        RepositoryInstance<string, IController> Controllers { get; set; }
        Guid PersistentId { get; set; }
        bool IsConnected { get; }
        NameValueCollection QueryString { get; set; }
        NameValueCollection Headers { get; set; }
        CookieCollection Cookies { get; set; }
        bool IsHandshakeDone { get; }
        Communication Communication { get; }
        event EventHandler OnAutoReconnectFailed;
        event EventHandler OnConnected;
        event EventHandler OnDisconnected;
        event EventHandler<OnErrorArgs> OnError;
        event EventHandler<Message> OnPing;
        event EventHandler<Message> OnPong;
        void Pong(byte[] data);
        void Ping(byte[] data);
        void SendControlFrame(FrameType frameType, byte[] data);
        IController Controller(string controller);
        void Disconnect();
        void SetAutoReconnect(int timeoutInMs = 5000);
        void Reconnect();
        void Open();
        void FireOnDisconnected();
        void FireOnMessage(IMessage message);
        void FireOnBlob(IMessage message);
        void FireError(Exception ex);
        void FireError(OnErrorArgs args);
    }

}
