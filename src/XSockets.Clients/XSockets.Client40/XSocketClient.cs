using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XSockets.Client40.Common.Event.Arguments;
using XSockets.Client40.Common.Interfaces;
using XSockets.Client40.Globals;
using XSockets.Client40.Helpers;
using XSockets.Client40.Model;
using XSockets.Client40.Protocol;
using XSockets.Client40.Protocol.Handshake.Builder;
using XSockets.Client40.Utility.Storage;
using XSockets.Client40.Wrapper;

namespace XSockets.Client40
{
    /// <summary>
    /// A client for communicating with XSockets over pub/sub
    /// </summary>
    public partial class XSocketClient : IXSocketClient
    {
        private static object locker = new object();
        public IXSocketJsonSerializer Serializer { get; set; }
        public RepositoryInstance<string, IController> Controllers { get; set; }

        public Guid PersistentId { get; set; }

        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;
        public event EventHandler<OnErrorArgs> OnError;
        public event EventHandler<Message> OnPing;
        public event EventHandler<Message> OnPong;

        public bool IsConnected
        {
            get
            {
                return this.Socket != null && this.Socket.Socket.Connected && this.IsHandshakeDone;
            }
        }

        public NameValueCollection QueryString { get; set; }

        public NameValueCollection Headers { get; set; }

        public CookieCollection Cookies { get; set; }

        public bool IsHandshakeDone { get; private set; }

        public ISocketWrapper Socket { get; private set; }

        public string Url { get; private set; }

        private EndPoint _remoteEndPoint;

        private IXFrameHandler _frameHandler;

        private string _origin;
        private bool _isSecure;
        private X509Certificate2 _certificate;

        private Socket _proxySocket;

        public void SetProxy(IWebProxy proxy)
        {
            var proxyRequest = (HttpWebRequest)WebRequest.Create(this._origin);
            proxyRequest.Proxy = proxy;
            var proxyResonse = (HttpWebResponse)proxyRequest.GetResponse();
            var responseStream = proxyResonse.GetResponseStream();
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            if (responseStream == null) return;
            var rsType = responseStream.GetType();
            var connectionProperty = rsType.GetProperty("Connection", flags);
            var connection = connectionProperty.GetValue(responseStream, null);
            var connectionType = connection.GetType();
            var props = connectionType.GetProperty("NetworkStream", flags);
            var ns = props.GetValue(connection, null);
            var nsType = ns.GetType();
            var socketProperty = nsType.GetProperty("Socket", flags);
            var temp = (Socket)socketProperty.GetValue(ns, null);
            temp.Disconnect(true);
            this._proxySocket = temp;
        }

        public XSocketClient(string url, string origin, params string[] controllers)
        {
            this.Headers = new NameValueCollection();
            this.QueryString = new NameValueCollection();
            this.Cookies = new CookieCollection();

            this.OnConnected += OnSocketConnected;

            var uri = new Uri(url);
            var instanceController = uri.AbsolutePath.Remove(0, 1).ToLower();

            this.Controllers = new RepositoryInstance<string, IController>();

            this.Serializer = new XSocketJsonSerializer();

            this.Url = url;
            this._origin = origin;


            if (!string.IsNullOrEmpty(instanceController))
            {
                this.Controllers.AddOrUpdate(instanceController, new Controller(this, instanceController));
            }

            foreach (var controller in controllers)
            {
                this.Controllers.AddOrUpdate(controller.ToLower(), new Controller(this, controller));
            }
        }

        public void AddClientCertificate(X509Certificate2 certificate)
        {
            this._isSecure = true;
            this._certificate = certificate;
        }

        public void Pong(byte[] data)
        {
            this.SendControlFrame(FrameType.Pong, data);
        }
        public void Ping(byte[] data)
        {
            this.SendControlFrame(FrameType.Ping, data);
        }

        private void SendControlFrame(FrameType frameType, byte[] data)
        {
            try
            {
                var pongFrame = GetDataFrame(frameType, data);

                Socket.Send(pongFrame.ToBytes(), () => { }, (ex) => { Disconnect(); });
            }
            catch
            {
                Disconnect();
            }
        }

        private void OnSocketConnected(object sender, EventArgs args)
        {
            foreach (var ctrl in this.Controllers.GetAll())
            {
                ctrl.Invoke(Constants.Events.Controller.Init, new { Init = true });
            }
        }

        public IController Controller(string controller)
        {
            controller = controller.ToLower();
            if (!this.Controllers.ContainsKey(controller))
                this.Controllers.AddOrUpdate(controller, new Controller(this, controller));
            return Controllers.GetById(controller);
        }

        private void FireOnDisconnected()
        {
            lock (locker)
            {
                if (!this.IsHandshakeDone) return;
                this.IsHandshakeDone = false;
                foreach (var controller in this.Controllers.GetAll())
                {
                    controller.Close();
                }
                if (this.OnDisconnected != null)
                    this.OnDisconnected.Invoke(this, null);

                if (this.Socket != null)
                    this.Socket.Dispose();
            }
        }

        public void Disconnect()
        {
            var frame = GetDataFrame(FrameType.Close, Encoding.UTF8.GetBytes(""));
            Socket.Send(frame.ToBytes(), () => this.FireOnDisconnected(), err => { });
        }

        public void Reconnect()
        {
            this.Open();
        }

        public void Open()
        {
            if (this.IsConnected) return;


            var connectionstring = GetConnectionstring();

            var handshake = new Rfc6455Handshake(connectionstring, _origin, this);

            SetRemoteEndpoint();

            ConnectSocket();

            var r = DoSynchronHandshake(handshake).Result;

            if (r)
                this.Connected();
            else
                throw new Exception("Could not connect to server");

        }

        private string GetConnectionstring()
        {
            var connectionstring = this.Url;

            if (this.PersistentId != Guid.Empty)
            {
                this.QueryString.Add(Constants.Connection.Parameters.PersistentId, this.PersistentId.ToString());
            }

            connectionstring += this.QueryString.ConstructQueryString();

            return connectionstring;
        }

        private void SetRemoteEndpoint()
        {
            var url = new Uri(this.Url);
            IPAddress ipAddress;
            if (!IPAddress.TryParse(url.Host, out ipAddress))
            {
                var addr = Dns.GetHostAddresses(url.Host);
                if (addr.Any(p => p.AddressFamily == AddressFamily.InterNetwork))
                    _remoteEndPoint = new IPEndPoint(addr.First(p => p.AddressFamily == AddressFamily.InterNetwork), url.Port);
            }
            else
            {
                _remoteEndPoint = new IPEndPoint(ipAddress, url.Port);
            }
        }

        private void ConnectSocket()
        {
            if (this._proxySocket == null)
            {
                var socket = new Socket(_remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(_remoteEndPoint);

                if (this._isSecure)
                    Socket = new SocketWrapper(socket, this._certificate);
                else
                    Socket = new SocketWrapper(socket);

            }
            else
            {
                var socket = this._proxySocket;

                var ca = new SocketAsyncEventArgs();
                ca.RemoteEndPoint = _remoteEndPoint;
                ca.Completed += (sender, args) => { Socket = new SocketWrapper((Socket)sender); };

                socket.ConnectAsync(ca);
                Thread.Sleep(2000);
            }
        }

        private Task<bool> DoSynchronHandshake(Rfc6455Handshake handshake)
        {
            var waiter = DoHandshake(handshake);
            waiter.Start();
            return waiter;
        }
        private Task<bool> DoHandshake(Rfc6455Handshake handshake)
        {

            return new Task<bool>(() =>
            {
                var cts = new CancellationTokenSource();
                var token = cts.Token;

                var working = true;

                var buffer = new byte[1024];
                Socket.Send(Encoding.UTF8.GetBytes(handshake.ToString()), () => Socket.Receive(buffer, r =>
                {
                    Receive();
                    IsHandshakeDone = true;

                    foreach (var ctrl in this.Controllers.GetAll())
                    {
                        ctrl.BindUnboundSubscriptions();
                    }
                    working = false;
                }, err => FireOnDisconnected()),
                            err => FireOnDisconnected());


                while (working)
                {
                    if (token.IsCancellationRequested)
                        return false;
                    Thread.Sleep(1);
                }
                return true;
            });


        }

        private void Connected()
        {
            if (this.OnConnected != null)
                this.OnConnected.Invoke(this, null);
        }

        protected virtual void FireOnMessage(IMessage message)
        {
            try
            {
                var controller = this.Controllers.GetById(message.Controller);
                controller.FireOnMessage(message);
            }
            catch (Exception ex) // Will dispatch to OnError on exception
            {
                if (this.OnError != null) this.OnError.Invoke(this, new OnErrorArgs(ex));
            }
        }
        protected virtual void FireOnBlob(IMessage message)
        {
            try
            {
                var controller = this.Controllers.GetById(message.Controller.ToLower());
                controller.FireOnBlob(message);
            }
            catch (Exception ex)
            {
                if (this.OnError != null) this.OnError.Invoke(this, new OnErrorArgs(ex));
            }
        }

        public virtual void FireError(Exception ex)
        {
            this.FireError(new OnErrorArgs(ex));
        }

        public virtual void FireError(OnErrorArgs args)
        {
            if (this.OnError != null) this.OnError.Invoke(this, args);
        }
    }
}