using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.IO;

namespace XSockets.Client40.Common.Interfaces
{
    public interface ISocketWrapper
    {
        bool Connected { get; set; }
        string RemoteIpAddress { get; }
        Stream Stream { get; }

        Task<ISocketWrapper> Accept(Action<ISocketWrapper> callback, Action<Exception> error);
        Task Send(byte[] buffer, Action callback, Action<Exception> error);
        Task<int> Receive(byte[] buffer, Action<int> callback, Action<Exception> error, int offset = 0);
        Task AuthenticateAsClient(X509Certificate2 certificate);
        void Dispose();
        void Close();

        void Bind(EndPoint ipLocal);
        void Listen(int backlog);
        System.Net.Sockets.Socket Socket { get; set; }
    }
}