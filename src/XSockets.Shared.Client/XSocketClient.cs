namespace XSockets
{
    using System;
    using System.Net;
#if !WINDOWS_UWP && !WINDOWS_PHONE_APP
    using System.Security.Cryptography.X509Certificates;
    using System.Reflection;
#endif
    using System.Text;
    using System.Threading.Tasks;
    using Common.Event.Arguments;
    using Common.Interfaces;
    using Globals;
    using Helpers;
    using Model;
    using Protocol;
    using Utility.Storage;
    using Wrapper;

    /// <summary>
    /// A client for communicating with XSockets over pub/sub and rpc
    /// </summary>
    public partial class XSocketClient : IXSocketClient
    {
        private static readonly object Locker = new object();

        public int QoSRetryInterval { get; set; }

        /// <summary>
        /// When true the client will try to reconnect when the connection is closed
        /// </summary>
        private bool AutoReconnect { get; set; }
        private int _autoReconnectTimeout;

        /// <summary>
        /// If true the client will ping the server and close the connection if a pong is not received
        /// </summary>
        public bool AutoHeartbeat { get; set; }
        private int _autoHeartbeatTimeout;

        private DateTime LastPong;
#if !WINDOWS_UWP && !WINDOWS_PHONE_APP
        private X509Certificate2 _certificate;

#endif
#if !WINDOWS_UWP && !WINDOWS_PHONE_APP
        public System.Net.Sockets.Socket ProxySocket;
#endif
        public string Origin { get; set; }
        public IXSocketJsonSerializer Serializer { get; set; }
        public RepositoryInstance<string, IController> Controllers { get; set; }

        public RepositoryInstance<int, byte[]> QoSRepository { get; set; }

        public Guid PersistentId { get; set; }

        public event EventHandler OnAutoReconnectFailed;
        public event EventHandler OnConnected;
        public event EventHandler OnAuthenticationFailed;
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

        /// <summary>
        /// Add values to pass with the connection.
        /// These can be accessed in the server as parameters on the ConnectionContext
        /// (You have to add these before you open the connection)
        /// </summary>
        public NameValueCollection QueryString { get; set; }

        /// <summary>
        /// Add headers to the connection.
        /// The headers can be accessed on the ConnectionContext in the server
        /// Headers have to be added before the connections is opened.
        /// </summary>
        public NameValueCollection Headers { get; set; }

        /// <summary>
        /// Set cookies on the connection.
        /// The cookies can be accessed on the ConnectionContext in the server
        /// Cookies have to be added before the connection is opened
        /// </summary>
        public CookieCollection Cookies { get; set; }

        public bool IsHandshakeDone { get; internal set; }

        /// <summary>
        /// Transport for communication
        /// </summary>
        public Communication Communication { get; private set; }

        private string _url;

        public XSocketClient(string url, string origin, params string[] controllers)
        {
            this.QoSRetryInterval = 5000;
            this._url = url;

            this.Origin = origin;
            this.Headers = new NameValueCollection();
            this.QueryString = new NameValueCollection();
            this.QueryString.Add("qos", "1");
            this.Cookies = new CookieCollection();

            this.Controllers = new RepositoryInstance<string, IController>();
            this.QoSRepository = new RepositoryInstance<int, byte[]>();

            this.Serializer = new XSocketJsonSerializer();

            foreach (var controller in controllers)
            {
                this.Controllers.AddOrUpdate(controller.ToLower(), new Controller(this, controller));
            }

            this.NetworkWatcher();

            Task.Run(async () =>
            {
                while (true)
                {
                    if (this.IsConnected)
                        foreach (var m in this.QoSRepository.GetAll())
                        {
                            try
                            {
                                await this.Communication.SendAsync(m, () => { });
                            }
                            catch { }
                        }

                    await Task.Delay(this.QoSRetryInterval);
                }
            });
        }
#if !WINDOWS_UWP && !WINDOWS_PHONE_APP
        public async Task SetProxy(IWebProxy proxy)
        {
            try
            {
                var proxyRequest = WebRequest.Create(this.Origin);
                proxyRequest.Proxy = proxy;
                //proxyRequest.UseDefaultCredentials = true;
                proxyRequest.Credentials = proxy.Credentials;
                var proxyResponse = await proxyRequest.GetResponseAsync();
                //var proxyResonse = (HttpWebResponse)proxyRequest.GetResponseAsync();//.GetResponse();
                var responseStream = proxyResponse.GetResponseStream();
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
                var temp = (System.Net.Sockets.Socket)socketProperty.GetValue(ns, null);
                temp.Disconnect(true);
                this.ProxySocket = temp;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public virtual void AddClientCertificate(X509Certificate2 certificate)
        {
            this._certificate = certificate;
        }
#endif
        private void OnSocketDisconnected(object sender, EventArgs eventArgs)
        {
            FireOnDisconnected();
        }

        public virtual async Task Pong(byte[] data)
        {
            await this.SendControlFrame(FrameType.Pong, data);
        }
        public virtual async Task Ping(byte[] data)
        {
            await this.SendControlFrame(FrameType.Ping, data);
        }

        public async Task SendControlFrame(FrameType frameType, byte[] data)
        {
            try
            {
                var pongFrame = this.Communication.GetDataFrame(frameType, data);
                await Communication.SendAsync(pongFrame.ToBytes(), () => { });
            }
            catch
            {
                await Disconnect();
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

        public async Task FireOnDisconnected()
        {
            if (!this.IsHandshakeDone) return;
            this.IsHandshakeDone = false;

            this.StopHeartbeat();

            foreach (var controller in this.Controllers.GetAll())
            {
                controller.FireClosed();
                controller.Close();
            }

            if (this.OnDisconnected != null)
                this.OnDisconnected.Invoke(this, null);

            if (this.AutoReconnect)
            {
                await Task.Factory.StartNew(async () =>
                {
                    while (!this.IsConnected && this.AutoReconnect)
                    {
                        await Task.Delay(this._autoReconnectTimeout);
                        try
                        {
                            if (!this.IsConnected)
                                if (await this.Reconnect() == false)
                                    if (this.OnAutoReconnectFailed != null)
                                        this.OnAutoReconnectFailed.Invoke(this, null);
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

        public virtual async Task Disconnect()
        {
            var frame = this.Communication.GetDataFrame(FrameType.Close, Encoding.UTF8.GetBytes(""));
            await Communication.SendAsync(frame.ToBytes(), async () => { await this.Communication.Disconnect(); });
        }

        /// <summary>
        /// Call before opening a connection. By default CleanSession is false.
        /// Having the value set to true will make the server persist messages sent while the client is offline and then send them once the client is back online.
        /// </summary>
        public virtual void SetCleanSession(bool value)
        {
            if (this.QueryString.ContainsKey("cs"))
                this.QueryString.Remove("cs");

            if (value)
                this.QueryString.Add("cs", "1");
        }

        ///// <summary>       
        ///// Enable/Disable Quality Of Service for the connection. 
        ///// Call this method before calling Open since the value is passed within the handshake.
        ///// QoS is enabled by default.
        ///// </summary>
        ///// <param name="value"></param>
        //public virtual void SetQualityOfService(bool value)
        //{
        //    if(value)
        //        if(this.QueryString.ContainsKey("qos"))
        //            this.QueryString.Remove("qos");
        //    else
        //        this.QueryString.Add("qos", "1");
        //}

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

        public virtual async Task<bool> Reconnect()
        {
            return await this.Open();
        }

        /// <summary>
        /// Will return true if the connection was a success, otherwise false
        /// AutoReconnect will only work if this return true.
        /// Handle the initial retry logic in your custom code
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> Open()
        {

            var uri = new Uri(this._url);
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
            this.Communication.OnError += (s, e) => { this.FireError(e); };
            this.Communication.OnConnected += OnSocketConnected;
            this.Communication.OnDisconnected += OnSocketDisconnected;
#if (!WINDOWS_UWP && !WINDOWS_PHONE_APP)
            return await Communication.Connect(this._certificate);
#else
            return await Communication.Connect();
#endif
        }

        private void Connected()
        {
            this.IsHandshakeDone = true;
            if (this.OnConnected != null)
                this.OnConnected.Invoke(this, null);

            this.StartHeartbeat(this._autoHeartbeatTimeout);
        }

        public virtual async Task FireOnMessage(IMessage message)
        {
            try
            {
                if (message.Topic == Globals.Constants.Events.AuthenticationFailed)
                {
                    if (this.OnAuthenticationFailed != null)
                    {
                        this.OnAuthenticationFailed.Invoke(this, null);
                    }
                    this.AutoReconnect = false;
                    await this.Disconnect();
                    return;
                }

                if (message.Topic == Constants.Events.Error && message.Controller == string.Empty)
                {

                    if (this.OnError != null)
                    {
                        OnErrorArgs errArgs = null;
                        try
                        {
                            errArgs = new OnErrorArgs(this.Serializer.DeserializeFromString<Exception>(message.Data));
                        }
                        catch
                        {
                            errArgs = new OnErrorArgs(message.Data);
                        }

                        this.OnError.Invoke(this, errArgs);
                    }
                    return;
                }

                var controller = this.Controllers.GetById(message.Controller);
                if (!await this.HandleQoS(message, controller)) return;
                controller.FireOnMessage(message);
            }
            catch (Exception ex) // Will dispatch to OnError on exception
            {

                if (this.OnError != null) this.OnError.Invoke(this, new OnErrorArgs(ex));
            }
        }
        public virtual async Task FireOnBlob(IMessage message)
        {
            try
            {
                //TODO: Handle QoS
                if (message.Topic == Globals.Constants.Events.AuthenticationFailed)
                {
                    if (this.OnAuthenticationFailed != null) {
                        this.OnAuthenticationFailed.Invoke(this, null);
                    }
                    this.AutoReconnect = false;
                    await this.Disconnect();
                    return;
                }

                var controller = this.Controllers.GetById(message.Controller.ToLower());
                if (!await this.HandleQoS(message, controller)) return;
                controller.FireOnBlob(message);
            }
            catch (Exception ex)
            {
                if (this.OnError != null) this.OnError.Invoke(this, new OnErrorArgs(ex));
            }
        }
        public virtual async Task<bool> HandleQoS(IMessage m, IController controller)
        {
            if (m.QoS > QoS.FireAndForget)
            {
                switch (m.QoS)
                {
                    case QoS.AtLeastOnce:
                        //QoS = 1
                        if (m.Topic == Constants.Events.QoS.MsgAck)
                        {
                            //server confirmed message, release message from storage to avoid/stop re-send
                            this.QoSRepository.Remove(m.Id);
                            return false;
                        }
                        else
                        {
                            var t = m.Topic;
                            m.Topic = Constants.Events.QoS.MsgAck;
                            await controller.Invoke(m, true);
                            m.Topic = t;
                            m.QoS = QoS.FireAndForget;
                            //server sent message, confirm message with msgack
                            //await controller.Invoke(Constants.Events.QoS.MsgAck, m);
                            return true;
                            //await this.Communication.SendAsync(.Invoke(m, Constants.Events.QoS.MsgAck);
                        }
                        break;
                        //case QoS.ExactlyOnce:
                        //    throw new Exception("QoS with level 2 is not yet supported");
                        //    //QoS = 2
                        //    if (m.Topic == Constants.Events.QoS.MsgRel)
                        //    {
                        //        //client sent message release, respond with message completed (and release message from store)
                        //        //await controller.Invoke(m, Constants.Events.QoS.MsgComp);
                        //    }
                        //    else if (m.Topic == Constants.Events.QoS.MsgRec)
                        //    {
                        //        //message was confirmed received, send message release and wait for message completed
                        //        //await controller.Invoke(m, Constants.Events.QoS.MsgRel);
                        //    }
                        //    break;
                }
            }
            return true;
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