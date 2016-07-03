#if WINDOWS_UWP || WINDOWS_PHONE_APP

namespace XSockets.Wrapper
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Foundation;
    using Windows.Networking;
    using Windows.Networking.Sockets;
    using Windows.Storage.Streams;
    using XSockets.Protocol.Handshake.Builder;

    /// <summary>
    /// Shared communication for win8, wp, win10
    /// </summary>
    public partial class Communication
    {
        private StreamSocket _clientSocket;
        private DataWriter _writer;
        private DataReader _reader;
        private HostName _serverHost;
        private byte[] _readBuffer;
        private async Task OnSocketConnected()
        {
            try
            {
                _writer = new DataWriter(_clientSocket.OutputStream);

                var connectionstring = GetConnectionstring();

                var handshake = new Rfc6455Handshake(connectionstring, this.Client.Origin, this.Client);

                _writer.WriteString(handshake.ToString());
                await _writer.StoreAsync();

                //read handshake
                _reader = new DataReader(_clientSocket.InputStream);
                _reader.InputStreamOptions = InputStreamOptions.Partial;

                var data = _reader.LoadAsync(1024);

                data.Completed = async (info, status) =>
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
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            ((Communication) this)._readBuffer = new byte[1024 * 320];
                            Task.Factory.StartNew(Read);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                            if (this.OnConnected != null)
                                this.OnConnected.Invoke(this, null);
                            return;

                        case AsyncStatus.Error:
                            if (this.OnError != null) this.OnError.Invoke(this, new Exception("Error when doing handshake"));
                            await this.Disconnect();
                            break;
                        case AsyncStatus.Canceled:
                            await this.Disconnect();
                            break;
                    }
                };
            }
            catch
            {
            }
        }
        public async Task<bool> Connect()
        {
            try
            {
                if (Connected) return true;
                _clientSocket = new StreamSocket();
                _serverHost = new HostName(_host);

                System.Diagnostics.Debug.WriteLine(_serverHost);
                //var secLevel = (this.IsSecure) ? SocketProtectionLevel.Tls12 : SocketProtectionLevel.PlainSocket;

                await _clientSocket.ConnectAsync(_serverHost, _port);

                await this.OnSocketConnected();
                return true;
            }
            catch (Exception)
            {
                if (this.Client.AutoReconnect)
                {
                    await Task.Delay(this.Client.AutoReconnectTimeout);
                    return await this.Client.Open(); //false;
                }
                else
                {
                    return false;
                }
            }
        }
        public void Read()
        {
            try
            {
                while(true)
                {
                    if (!Connected) return;

                    var op = _reader.LoadAsync(1024 * 320);

                    op.Completed = async (info, status) =>
                    {
                        switch (status)
                        {
                            case AsyncStatus.Completed:
                                uint byteCount = _reader.UnconsumedBufferLength;

                                //byte[] buffer = new byte[byteCount];
                                _reader.ReadBytes(((Communication) this)._readBuffer);

                                if (byteCount <= 0)
                                    await this.Disconnect();
                                else
                                {
                                    FrameHandler.Receive(new ArraySegment<byte>(((Communication) this)._readBuffer, 0, (int)byteCount));
                                    Array.Clear(((Communication) this)._readBuffer, 0, (int)byteCount);                                  
                                }
                                break;
                            case AsyncStatus.Error:
                                await this.Disconnect();
                                break;
                            case AsyncStatus.Canceled:
                                await this.Disconnect();
                                break;
                        }
                    };
                }
            }
            catch
            {
                this.Disconnect();
            }
        }
        public async Task Disconnect()
        {
            try
            {

                if (!this.Connected) return;
                this.Connected = false;

                await _writer.StoreAsync();
                _writer.DetachStream();
                _writer.Dispose();
                _writer = null;

                _reader.Dispose();
                _reader = null;

                _clientSocket.Dispose();
                _clientSocket = null;


                if (this.OnDisconnected != null)
                    this.OnDisconnected.Invoke(this, null);

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
#endif