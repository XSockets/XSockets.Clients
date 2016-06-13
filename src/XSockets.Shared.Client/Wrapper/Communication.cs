
namespace XSockets.Wrapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
#if !WINDOWS_PHONE_APP && !WINDOWS_UWP
    using System.Net.Sockets;
#endif
    using System.Text;
    using System.Threading.Tasks;
    using XSockets.Common.Event.Arguments;
    using XSockets.Common.Interfaces;
    using XSockets.Globals;
    using XSockets.Helpers;
    using XSockets.Model;
    using XSockets.Protocol;
    using XSockets.Protocol.FrameBuilders;
    using XSockets.Protocol.Readers;

    public partial class Communication
    {        
        private static object Locker = new object();         
        private ReadState readState;
        public event EventHandler OnConnected;
        public event EventHandler<Exception> OnError;
        public event EventHandler OnDisconnected;
        public event EventHandler<Message> OnPing;
        public event EventHandler<Message> OnPong;

        public bool Connected { get; private set; }
        private readonly string _host;
        private readonly string _port;
        private readonly Uri _uri;

        private bool IsSecure { get { return this._uri.Scheme.ToLower() == "wss"; } }

        private IXSocketClient Client { get; set; }
        protected IXFrameHandler FrameHandler { get; set; }

        public Communication(IXSocketClient client, Uri uri)
        {
            Client = client;
            this._uri = uri;
            this._host = uri.Host;
            this._port = uri.Port.ToString();
            readState = new ReadState();
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

#if !WINDOWS_UWP && !WINDOWS_PHONE_APP
        private IPEndPoint GetRemoteEndpoint()
        {            
            IPAddress ipAddress;
            if (!IPAddress.TryParse(_uri.Host, out ipAddress))
            {
                var addr = Dns.GetHostAddresses(_uri.Host);
                if (addr.Any(p => p.AddressFamily == AddressFamily.InterNetwork))
                    return new IPEndPoint(addr.First(p => p.AddressFamily == AddressFamily.InterNetwork), _uri.Port);
                else
                    throw new Exception("Could not get remote endpoint");
            }
            else
            {
                return new IPEndPoint(ipAddress, _uri.Port);
            }
        }
#endif
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

        internal async Task UpdateNetworkState(NetworkState state)
        {
            switch (state)
            {
                case NetworkState.Offline:                                            
                        await this.Disconnect();                    
                    break;                
            }
        }
    }
}