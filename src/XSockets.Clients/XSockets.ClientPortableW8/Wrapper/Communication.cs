using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using XSockets.ClientPortableW8.Common.Event.Arguments;
using XSockets.ClientPortableW8.Common.Interfaces;
using XSockets.ClientPortableW8.Globals;
using XSockets.ClientPortableW8.Helpers;
using XSockets.ClientPortableW8.Model;
using XSockets.ClientPortableW8.Protocol;
using XSockets.ClientPortableW8.Protocol.FrameBuilders;
using XSockets.ClientPortableW8.Protocol.Handshake.Builder;
using XSockets.ClientPortableW8.Protocol.Readers;

namespace XSockets.ClientPortableW8.Wrapper
{
    public class Communication
    {
        private static object Locker = new object();
        private StreamSocket _clientSocket;
        private DataWriter _writer;
        private DataReader _reader;
        private HostName _serverHost;
        private ReadState readState;

        public event EventHandler<string> OnMessage;
        public event EventHandler OnConnected;
        private event EventHandler SocketConnected;
        public event EventHandler<Exception> OnError;
        public event EventHandler OnDisconnected;
        public event EventHandler<Message> OnPing;
        public event EventHandler<Message> OnPong;

        public bool Connected { get; private set; }
        private readonly string _host;
        private readonly string _port;
        private readonly Uri _uri;
        //For future implementation of SSL in this client
        private bool IsSecure { get { return this._uri.Scheme == "wss"; } }
        
        private IXSocketClient Client { get; set; }
        protected IXFrameHandler FrameHandler { get; set; }

        public Communication(IXSocketClient client, Uri uri)
        {
            Client = client;
            this._uri = uri;
            this._host = uri.Host;
            this._port = uri.Port.ToString();
            readState = new ReadState();
            SocketConnected += OnSocketConnected;
        }

        private string GetConnectionstring()
        {
            var connectionstring = string.Format("{0}://{1}:{2}", this._uri.Scheme, this._host, this._port);

            if (this.Client.PersistentId != Guid.Empty)
            {
                this.Client.QueryString.Remove(Constants.Connection.Parameters.PersistentId);
                this.Client.QueryString.Add(Constants.Connection.Parameters.PersistentId, this.Client.PersistentId.ToString());
            }

            connectionstring += this.Client.QueryString.ConstructQueryString();

            return connectionstring;
        }

        private void OnSocketConnected(object sender, EventArgs eventArgs)
        {
            try
            {


                _writer = new DataWriter(_clientSocket.OutputStream);

                var connectionstring = GetConnectionstring();

                var handshake = new Rfc6455Handshake(connectionstring, this.Client.Origin, this.Client);

                _writer.WriteString(handshake.ToString());
                _writer.StoreAsync();

                //read handshake
                _reader = new DataReader(_clientSocket.InputStream);
                _reader.InputStreamOptions = InputStreamOptions.Partial;

                var data = _reader.LoadAsync(1024);

                data.Completed = (info, status) =>
                {
                    switch (status)
                    {
                        case AsyncStatus.Completed:
                            //read complete message
                            uint byteCount = _reader.UnconsumedBufferLength;

                            byte[] bytes = new byte[byteCount];
                            _reader.ReadBytes(bytes);

                            var r = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                            //Debug.WriteLine(r);

                            Connected = true;
                            //Start receive thread
                            FrameHandler = CreateFrameHandler();
                            Task.Factory.StartNew(Read);

                            if (this.OnConnected != null)
                                this.OnConnected.Invoke(this, null);

                            return;

                        case AsyncStatus.Error:
                            if (this.OnError != null) this.OnError.Invoke(this, new Exception("Error when doing handshake"));
                            this.Disconnect();
                            break;
                        case AsyncStatus.Canceled:
                            this.Disconnect();
                            break;
                    }
                };
            }
            catch
            {
            }
        }

        public void Connect()
        {
            try
            {
                if (Connected) return;
                _clientSocket = new StreamSocket();
                _serverHost = new HostName(_host);
                //var secLevel = (this.IsSecure) ? SocketProtectionLevel.Ssl : SocketProtectionLevel.PlainSocket;
                
                var op = _clientSocket.ConnectAsync(_serverHost, _port);

                op.Completed = (info, status) =>
                {
                    switch (status)
                    {
                        case AsyncStatus.Completed:
                            this.SocketConnected.Invoke(this, null);
                            break;
                        case AsyncStatus.Error:
                            this.Disconnect();
                            break;
                        case AsyncStatus.Canceled:
                            this.Disconnect();
                            // Read is not cancelled in this sample.
                            break;
                    }
                };
            }
            catch (Exception)
            {
                this.Disconnect();
            }
        }

        private IXFrameHandler CreateFrameHandler()
        {

            return Create((payload, op) =>
            {
                switch (op)
                {
                    case FrameType.Text:
                        this.Client.FireOnMessage(this.Client.Deserialize<Message>(Encoding.UTF8.GetString(payload.ToArray())));
                        break;
                    case FrameType.Binary:
                        this.Client.FireOnBlob(new Message(payload, MessageType.Binary));
                        break;
                    case FrameType.Ping:

                        this.Client.SendControlFrame(FrameType.Pong, payload.ToArray());
                        if (this.OnPing != null)
                            this.OnPing(this, new Message(payload, MessageType.Ping));
                        break;
                    case FrameType.Pong:
                        this.Client.SendControlFrame(FrameType.Ping, payload.ToArray());
                        if (this.OnPong != null)
                            this.OnPong(this, new Message(payload, MessageType.Pong));
                        break;
                    case FrameType.Close:
                        this.Client.FireOnDisconnected();
                        break;
                }
            });

        }

        private IXFrameHandler Create(Action<IList<byte>, FrameType> onCompleted)
        {
            return new Rfc6455FrameHandler()
            {
                ReceiveData =
                    d => ProcessData(d, onCompleted)
            };
        }


        public void Read()
        {
            if (!Connected) return;

            var op = _reader.LoadAsync(1024);

            op.Completed = (info, status) =>
            {
                switch (status)
                {
                    case AsyncStatus.Completed:
                        //read complete message
                        uint byteCount = _reader.UnconsumedBufferLength;

                        byte[] buffer = new byte[byteCount];
                        _reader.ReadBytes(buffer);

                        if (byteCount <= 0)
                            this.Disconnect();
                        else
                        {
                            FrameHandler.Receive(new ArraySegment<byte>(buffer, 0, (int)byteCount));
                            Read();
                        }
                        break;
                    case AsyncStatus.Error:
                        this.Disconnect();
                        break;
                    case AsyncStatus.Canceled:
                        this.Disconnect();
                        // Read is not cancelled in this sample.
                        break;
                }
            };
        }

        /// <summary>
        /// Readstate when reading frames
        /// </summary>
        public class ReadState : IReadState
        {
            /// <summary>
            /// Ctor
            /// </summary>
            public ReadState()
            {
                Data = new List<byte>();
                FrameBytes = new List<byte>();
            }

            #region IReadState Members
            /// <summary>
            /// 
            /// </summary>
            public List<byte> Data { get; private set; }
            /// <summary>
            /// 
            /// </summary>
            public List<byte> FrameBytes { get; private set; }
            /// <summary>
            /// 
            /// </summary>
            public FrameType? FrameType { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int Length { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int BufferedIndex { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public bool IsFinal { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public virtual void Clear()
            {
                this.FrameType = null;
                this.FrameBytes.Clear();
                this.IsFinal = false;
                this.Length = 0;
                this.BufferedIndex = 0;
            }

            #endregion
        }

        private void ProcessData(ArraySegment<byte> data, Action<IList<byte>, FrameType> processFrame)
        {
            var buffer = new byte[data.Count];
            Array.Copy(data.Array, buffer, data.Count);
            readState.Data.AddRange(buffer);

            while (readState.Data.Count() > 2)
            {
                var bytesRead = readState.Data.Count;

                var reservedBits = (readState.Data[0] & 112);
                bool isFinal = (readState.Data[0] & 128) != 0;
                //bool isMasked = (readState.Data[1] & 128) != 0;
                var frameType = (FrameType)(readState.Data[0] & 15);

                if (reservedBits != 0)
                {
                    return;
                }


                int length = (readState.Data[1] & 127);
                int index = 2;
                switch (length)
                {
                    case 127:
                        if (bytesRead < index + 8)
                            return;
                        readState.Length = readState.Data.Skip(index).Take(8).ToArray().ToLittleEndianInt();
                        index += 8;
                        break;
                    case 126:
                        if (bytesRead < index + 2)
                            return;
                        readState.Length = readState.Data.Skip(index).Take(2).ToArray().ToLittleEndianInt();
                        index += 2;
                        break;
                    default:
                        readState.Length = length;
                        break;
                }

                if (readState.Data.Count < index + 4)
                    return;

                if (bytesRead < readState.Length + index) return;

                var payload = readState.Data.GetRange(index, readState.Length);

                readState.Data.RemoveRange(0, index + readState.Length);

                readState.FrameBytes.AddRange(payload);

                if (frameType != FrameType.Continuation)
                    readState.FrameType = frameType;

                if (!isFinal && readState.FrameType.HasValue) continue;

                if (!readState.FrameType.HasValue)
                {
                    throw new Exception("FrameType unknown");                    
                }
                processFrame(readState.FrameBytes, readState.FrameType.Value);

                readState.Clear();
            }
        }

        //private void FatalError(Exception ex)
        //{
        //    //
        //}

        public Rfc6455DataFrame GetDataFrame(FrameType frameType, byte[] payload)
        {
            var frame = new Rfc6455DataFrame
            {
                FrameType = frameType,
                IsFinal = true,
                IsMasked = true,
                MaskKey = new Random().Next(0, 34298),
                Payload = payload
            };
            return frame;
        }
        private Rfc6455DataFrame GetDataFrame(string payload)
        {
            return GetDataFrame(FrameType.Text, Encoding.UTF8.GetBytes(payload));
        }
       
        public void Disconnect()
        {
            try
            {
                lock (Locker)
                {
                    if (!this.Connected) return;

                    _writer.StoreAsync();
                    _writer.DetachStream();
                    _writer.Dispose();
                    _writer = null;
            
                    _reader.Dispose();
                    _reader = null;

                    _clientSocket.Dispose();
                    _clientSocket = null;

                    this.Connected = false;

                    if (this.OnDisconnected != null)
                        this.OnDisconnected.Invoke(this, null);
                }
            }
            catch
            {
                this.Connected = false;
                if (this.OnDisconnected != null)
                    this.OnDisconnected.Invoke(this, null);
            }
        }

        public async Task SendAsync(string json, Action callback)
        {
            if (!Connected) return;

            var frame = GetDataFrame(json);
            _writer.WriteBytes(frame.ToBytes());
            await _writer.StoreAsync();

            callback();
        }

        public async Task SendAsync(byte[] data, Action callback)
        {
            if (!Connected) return;

            _writer.WriteBytes(data);
            await _writer.StoreAsync();

            callback();
        }
    }
}
