using System;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using XSockets.Common.Event.Arguments;
using XSockets.Model;
using XSockets.Utility.Storage;

namespace XSockets.Common.Interfaces
{
    public interface IXSocketClient
    {
        event EventHandler OnAutoReconnectFailed;
        event EventHandler OnConnected;
        event EventHandler OnDisconnected;
        event EventHandler<OnErrorArgs> OnError;
        event EventHandler<Message> OnPing;
        event EventHandler<Message> OnPong;

        NameValueCollection QueryString { get; set; }
        NameValueCollection Headers { get; set; }
        CookieCollection Cookies { get; set; }

        IXSocketJsonSerializer Serializer { get; set; }
        Guid PersistentId { get; set; }

        RepositoryInstance<string, IController> Controllers { get; set; }
        IController Controller(string controller);

        bool IsConnected { get; }
        bool IsHandshakeDone { get; }
        bool AutoReconnect { get; set; }

        ISocketWrapper Socket { get; }
        string Url { get; }

        /// <summary>
        /// Timeout in MS
        /// </summary>
        int ConnectionTimeout { get; set; }

        void Disconnect();
        void Open();
        void Reconnect();
        void SetAutoReconnect(int interval = 5000);

        void SetProxy(IWebProxy proxy);
        void AddClientCertificate(X509Certificate2 certificate);

        void FireError(Exception ex);
        void FireError(OnErrorArgs args);
    }
}
