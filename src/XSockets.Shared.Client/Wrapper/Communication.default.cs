#if !WINDOWS_UWP && !WINDOWS_PHONE_APP

namespace XSockets.Wrapper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using Protocol.Handshake.Builder;

    public partial class Communication
    {
        private Stream _stream;
        private Socket _socket;
        private byte[] _readBuffer;

        private async Task OnSocketConnected()
        {
            try
            {
                var connectionstring = GetConnectionstring();

                var handshake = new Rfc6455Handshake(connectionstring, this.Client.Origin, this.Client);

                var b = Encoding.UTF8.GetBytes(handshake.ToString());
                await this._stream.WriteAsync(b, 0, b.Length);


                Connected = true;
                //Start receive thread
                FrameHandler = CreateFrameHandler();

                var data = new List<byte>(1024);
                var buffer = new byte[1024];
                ReadHandshake(data, buffer);
            }
            catch (Exception ex)
            {
                //Connection failed
                if (this.OnError != null)
                    this.OnError.Invoke(this, ex);
                await this.Client.FireOnDisconnected();
            }
        }

        private void ReadHandshake(List<byte> data, byte[] buffer)
        {
            var l = this._stream.Read(buffer, 0, buffer.Length);

            data.AddRange(buffer.Take(l));

            if (data.Count > 2)
            {
                if (this.OnConnected != null)
                    this.OnConnected.Invoke(this, null);
                //TODO: Managed thread instead of Task.Run?
                Task.Run(() =>
                {
                    try
                    {
                        this._readBuffer = new byte[1024 * 320];
                        Read();
                    }
                    catch { }
                });
            }
            else
            {
                ReadHandshake(data, buffer);
            }
        }

        public async Task<bool> Connect(X509Certificate2 certificate = null)
        {
            try
            {
                if (Connected) return true;


                this._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //this.Socket.Connect(GetRemoteEndpoint());
                // Connect using a timeout (5 seconds)

                IAsyncResult result = this._socket.BeginConnect(GetRemoteEndpoint(), null, null);
                bool success = result.AsyncWaitHandle.WaitOne(5000, true);

                if (!success || !this._socket.Connected)
                {
                    return false;
                }


                this._stream = new NetworkStream(this._socket);

                if (this.IsSecure)
                {
                    //SSL - Upgrade to SSL Stream, use cert if available
                    if (certificate != null)
                    {
                        await this.AuthenticateAsClient(certificate);
                    }
                    else
                    {
                        await this.AuthenticateAsClient();
                    }
                }

                await this.OnSocketConnected();

                return true;
            }
            catch
            {
                if(this.Client.AutoReconnect)
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
        public async Task AuthenticateAsClient()
        {
            var ssl = new SslStream(_stream, false, (sender, x509Certificate, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                    return true;

                return false;
            }, null);

            _stream = ssl;
            Func<AsyncCallback, object, IAsyncResult> begin =
                (cb, s) => ssl.BeginAuthenticateAsClient(this._uri.Host, cb, s);

            await Task.Factory.FromAsync(begin, ssl.EndAuthenticateAsClient, null);
        }

        public async Task AuthenticateAsClient(X509Certificate2 certificate)
        {
            var ssl = new SslStream(_stream, false, (sender, x509Certificate, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                    return true;

                return certificate.Equals(x509Certificate);
            }, null);

            //var tempStream = new SslStreamWrapper(ssl);
            _stream = ssl;//tempStream;
            Func<AsyncCallback, object, IAsyncResult> begin =
                (cb, s) => ssl.BeginAuthenticateAsClient(this._uri.Host,
                    new X509Certificate2Collection(certificate), SslProtocols.Tls, false, cb, s);

            await Task.Factory.FromAsync(begin, ssl.EndAuthenticateAsClient, null);
        }

        public void Read()
        {
            try
            {
                while (true)
                {
                    if (!Connected) return;

                    var l = _stream.Read(this._readBuffer, 0, this._readBuffer.Length);

                    if (l <= 0)
                    {
                        this.Disconnect();
                        return;
                    }

                    FrameHandler.Receive(new ArraySegment<byte>(this._readBuffer, 0, (int)l));
                    Array.Clear(this._readBuffer, 0, l);
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

                //if (!this.Connected) return;
                this.Connected = false;
                if(_stream != null) { 
                await _stream.FlushAsync();
                    _stream.Close();
                    _stream.Dispose();
                    _socket.Dispose();
                }
                await this.Client.FireOnDisconnected();
                if (this.OnDisconnected != null)
                    this.OnDisconnected.Invoke(this, EventArgs.Empty);

            }
            catch
            {
                this.Connected = false;
                if (this.OnDisconnected != null)
                    this.OnDisconnected.Invoke(this, EventArgs.Empty);
            }
        }
        public async Task SendAsync(string json, Action callback)
        {
            if (!Connected) return;

            var frame = GetDataFrame(json).ToBytes();
            await this._stream.WriteAsync(frame, 0, frame.Length);
            await this._stream.FlushAsync();
            if (callback != null)
                callback();
        }
        public async Task SendAsync(byte[] data, Action callback)
        {
            if (!Connected) return;

            await this._stream.WriteAsync(data, 0, data.Length);
            await this._stream.FlushAsync();
            if (callback != null)
                callback();
        }
    }
}
#endif