using System;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using XSockets.ClientIOS.Common.Event.Arguments;
using XSockets.ClientIOS.Model;
using XSockets.ClientIOS.Utility.Storage;

namespace XSockets.ClientIOS.Common.Interfaces
{
    public interface IXSocketClient
    {
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

        ISocketWrapper Socket { get; }
        string Url { get; }
                

        void Disconnect();
        void Open();
        void Reconnect();

        void SetProxy(IWebProxy proxy);
        void AddClientCertificate(X509Certificate2 certificate);

        void FireError(Exception ex);
        void FireError(OnErrorArgs args);
    }
}
