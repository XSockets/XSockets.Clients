
namespace XSockets.Common.Interfaces
{
    using System;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using XSockets.Common.Event.Arguments;
    using XSockets.Helpers;
    using XSockets.Model;
    using XSockets.Protocol;
    using XSockets.Utility.Storage;
    using XSockets.Wrapper;

    public interface IXSocketClient
    {
        string Origin { get; set; }
        bool AutoReconnect { get; }
        int AutoReconnectTimeout { get;  }
        IXSocketJsonSerializer Serializer { get; set; }
        RepositoryInstance<string, IController> Controllers { get; set; }
        RepositoryInstance<int, byte[]> QoSRepository { get; set; }
        Guid PersistentId { get; set; }
        bool IsConnected { get; }
        int QoSRetryInterval { get; set; }
        NameValueCollection QueryString { get; set; }
        NameValueCollection Headers { get; set; }
        CookieCollection Cookies { get; set; }
        //bool IsHandshakeDone { get; }
        Communication Communication { get; }
        event EventHandler OnConnectAttempt;
        event EventHandler OnAutoReconnectFailed;
        event EventHandler OnConnected;
        event EventHandler OnDisconnected;
        event EventHandler OnAuthenticationFailed;
        event EventHandler<OnErrorArgs> OnError;
        event EventHandler<Message> OnPing;
        event EventHandler<Message> OnPong;
        Task Pong(byte[] data);
        Task Ping(byte[] data);
        Task SendControlFrame(FrameType frameType, byte[] data);
        IController Controller(string controller);
        Task Disconnect();
        void SetAutoReconnect(int timeoutInMs = 5000);
        void SetCleanSession(bool value);
        void SetAutoHeartbeat(int timeoutInMs = 30000);
        void AddClientCertificate(X509Certificate2 certificate);
        Task SetProxy(IWebProxy proxy);
        Task<bool> Reconnect();
        Task<bool> Open();
        Task FireOnDisconnected();
        Task FireOnMessage(IMessage message);
        Task FireOnBlob(IMessage message);
        void FireError(Exception ex);
        void FireError(OnErrorArgs args);
    }

}
