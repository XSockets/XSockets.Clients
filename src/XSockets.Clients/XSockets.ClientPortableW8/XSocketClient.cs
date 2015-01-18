using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using XSockets.ClientPortableW8.Common.Event.Arguments;
using XSockets.ClientPortableW8.Common.Interfaces;
using XSockets.ClientPortableW8.Globals;
using XSockets.ClientPortableW8.Helpers;
using XSockets.ClientPortableW8.Model;
using XSockets.ClientPortableW8.Protocol;
using XSockets.ClientPortableW8.Utility.Storage;
using XSockets.ClientPortableW8.Wrapper;

namespace XSockets.ClientPortableW8
{    
    /// <summary>
    /// A client for communicating with XSockets over pub/sub and rpc
    /// </summary>
    public partial class XSocketClient : IXSocketClient
    {
        private static readonly object Locker = new object();
        public bool AutoReconnect { get; set; }
        private int _autoReconnectTimeout;
        public string Origin { get; set; }        
        public IXSocketJsonSerializer Serializer { get; set; }
        public RepositoryInstance<string, IController> Controllers { get; set; }

        public Guid PersistentId { get; set; }        

        public event EventHandler OnAutoReconnectFailed;
        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;
        public event EventHandler<OnErrorArgs> OnError;
        public event EventHandler<Message> OnPing;
        public event EventHandler<Message> OnPong;

        public bool IsConnected
        {
            get
            {
                return this.Communication != null && this.Communication.Connected;
            }
        }

        public NameValueCollection QueryString { get; set; }

        public NameValueCollection Headers { get; set; }

        public CookieCollection Cookies { get; set; }

        public bool IsHandshakeDone { get; private set; }

        public Communication Communication { get; private set; }

        //public string Url { get; private set; }

        //private IXFrameHandler _frameHandler;

        //private string _origin;
        //private bool _isSecure;
        
        //private Socket _proxySocket;

        //public void SetProxy(IWebProxy proxy)
        //{
        //    var proxyRequest = (HttpWebRequest)WebRequest.Create(this._origin);
        //    proxyRequest.Proxy = proxy;
        //    var proxyResonse = (HttpWebResponse)proxyRequest.GetResponse();
        //    var responseStream = proxyResonse.GetResponseStream();
        //    const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        //    if (responseStream == null) return;
        //    var rsType = responseStream.GetType();
        //    var connectionProperty = rsType.GetProperty("Connection", flags);
        //    var connection = connectionProperty.GetValue(responseStream, null);
        //    var connectionType = connection.GetType();
        //    var props = connectionType.GetProperty("NetworkStream", flags);
        //    var ns = props.GetValue(connection, null);
        //    var nsType = ns.GetType();
        //    var socketProperty = nsType.GetProperty("Socket", flags);
        //    var temp = (Socket)socketProperty.GetValue(ns, null);
        //    temp.Disconnect(true);
        //    this._proxySocket = temp;
        //}

        public XSocketClient(string url, string origin, params string[] controllers)//(string host, string port, string origin, params string[] controllers)
        {
            var uri = new Uri(url);

            this.Origin = origin;
            this.Headers = new NameValueCollection();
            this.QueryString = new NameValueCollection();
            this.Cookies = new CookieCollection();

            this.Communication = new Communication(this, uri);
            this.Communication.OnPing += (sender, message) =>
            {
                if (this.OnPing != null)
                    this.OnPing.Invoke(this, message);
            };
            this.Communication.OnPong += (sender, message) =>
            {
                if (this.OnPong != null)
                    this.OnPong.Invoke(this, message);
            };
            this.Communication.OnConnected += OnSocketConnected;
            this.Communication.OnDisconnected += OnSocketDisconnected;

            //this.OnHandshakeCompleted += XSocketClient_OnHandshakeCompleted;

            //var uri = new Uri(url);
            //var instanceController = uri.AbsolutePath.Remove(0, 1).ToLower();

            this.Controllers = new RepositoryInstance<string, IController>();

            this.Serializer = new XSocketJsonSerializer();

            //this.Url = url;
            //this._origin = origin;


            //if (!string.IsNullOrEmpty(instanceController))
            //{
            //    this.Controllers.AddOrUpdate(instanceController, new Controller(this, instanceController));
            //}

            foreach (var controller in controllers)
            {
                this.Controllers.AddOrUpdate(controller.ToLower(), new Controller(this, controller));
            }
        }

        private void OnSocketDisconnected(object sender, EventArgs eventArgs)
        {
            FireOnDisconnected();
        }

        //void XSocketClient_OnHandshakeCompleted(object sender, EventArgs e)
        //{
        //    //IsHandshakeDone = true;
        //    foreach (var ctrl in this.Controllers.GetAll())
        //    {
        //        ctrl.BindUnboundSubscriptions();
        //    }
        //}

        //public virtual void AddClientCertificate(X509Certificate2 certificate)
        //{
        //    this._isSecure = true;
        //    this._certificate = certificate;
        //}

        public virtual void Pong(byte[] data)
        {
            this.SendControlFrame(FrameType.Pong, data);
        }
        public virtual void Ping(byte[] data)
        {
            this.SendControlFrame(FrameType.Ping, data);
        }

        public async void SendControlFrame(FrameType frameType, byte[] data)
        {
            try
            {
                var pongFrame = this.Communication.GetDataFrame(frameType, data);

                await Communication.SendAsync(pongFrame.ToBytes(), () => { });
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
                ((Controller)ctrl).OpenController();
            }

            foreach (var ctrl in this.Controllers.GetAll())
            {
                ctrl.BindUnboundSubscriptions();
            }
            this.Connected();
        }

        public virtual IController Controller(string controller)
        {
            controller = controller.ToLower();
            if (!this.Controllers.ContainsKey(controller))
                this.Controllers.AddOrUpdate(controller, new Controller(this, controller));
            return Controllers.GetById(controller);
        }

        public void FireOnDisconnected()
        {
            lock (Locker)
            {
                if (!this.IsHandshakeDone) return;
                this.IsHandshakeDone = false;

                foreach (var controller in this.Controllers.GetAll())
                {
                    controller.FireClosed();
                    controller.Close();                        
                }                

                if (this.OnDisconnected != null)
                    this.OnDisconnected.Invoke(this, null);

                if (this.AutoReconnect)
                {
                    //TODO: Rewrite this to use 4.5 async/await...
                    Task.Factory.StartNew(async() =>
                    {
                        while (!this.IsConnected && this.AutoReconnect)
                        {
                            await Task.Delay(this._autoReconnectTimeout);
                            try
                            {
                                if (!this.IsConnected)
                                    this.Reconnect();
                            }
                            catch
                            {
                                if (this.OnAutoReconnectFailed != null)
                                    this.OnAutoReconnectFailed.Invoke(this, null);
                            }
                        }
                    });
                }
            }
        }

        public virtual async void Disconnect()
        {
            //this.Communication.Disconnect();
            var frame = this.Communication.GetDataFrame(FrameType.Close, Encoding.UTF8.GetBytes(""));
            await Communication.SendAsync(frame.ToBytes(), ()=> { this.Communication.Disconnect(); });//.ConfigureAwait(false);
        }


        public virtual void SetAutoReconnect(int timeoutInMs = 5000)
        {
            if (timeoutInMs <= 0)
            {
                AutoReconnect = false;
                _autoReconnectTimeout = 0;
            }
            else
            {
                AutoReconnect = true;
                _autoReconnectTimeout = timeoutInMs;
            }
        }

        public virtual void Reconnect()
        {
            this.Open();
        }

        public virtual void Open()
        {
            Communication.Connect();
        }                
        
        private void Connected()
        {
            this.IsHandshakeDone = true;
            if (this.OnConnected != null)
                this.OnConnected.Invoke(this, null);
        }

        public virtual void FireOnMessage(IMessage message)
        {
            try
            {
                if (message.Topic == Constants.Events.Error && message.Controller == string.Empty)
                {
                    if (this.OnError != null) this.OnError.Invoke(this, new OnErrorArgs(this.Serializer.DeserializeFromString<Exception>(message.Data)));
                    return;
                }

                var controller = this.Controllers.GetById(message.Controller);

                controller.FireOnMessage(message);
            }
            catch (Exception ex) // Will dispatch to OnError on exception
            {

                if (this.OnError != null) this.OnError.Invoke(this, new OnErrorArgs(ex));
            }
        }
        public virtual void FireOnBlob(IMessage message)
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