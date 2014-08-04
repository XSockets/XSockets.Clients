using System;
using System.Collections.Specialized;
using System.Text;
using XSockets.ClientAndroid.Common.Interfaces;

namespace XSockets.ClientAndroid.Protocol.Handshake.Builder
{
    internal class Rfc6455Handshake
    {
        public NameValueCollection Handshake { get; set; }

        private IXSocketClient _client;
        //private const string Handshake =
        //    "GET {0} HTTP/1.1\r\n" +
        //    //"Connection: Upgrade\r\n" +            
        //    "Host: {2}\r\n" +
        //    "Origin: {1}\r\n" +
        //    "Upgrade: websocket\r\n" +
        //    "Connection: Upgrade,Keep-Alive\r\n" +
        //    "Sec-WebSocket-Key: {3}\r\n" +
        //    "Sec-WebSocket-Version: 13\r\n" +
        //    "Sec-WebSocket-Protocol: XSocketsNET\r\n\r\n";//+
        //    //"{4}";

        private readonly string _host = String.Empty;
        private readonly string _origin = String.Empty;
        private readonly string _path = String.Empty;
        private string Key { get; set; }

        public Rfc6455Handshake(string url, string origin, IXSocketClient client)
        {
            _client = client;
            this.Handshake = new NameValueCollection();
            this.Handshake.Add("GET", "GET {0} HTTP/1.1");
            this.Handshake.Add("HOST", "Host: {0}");
            this.Handshake.Add("ORIGIN", "Origin: {0}");
            this.Handshake.Add("UPGRADE","Upgrade: websocket");
            this.Handshake.Add("CONNECTION", "Connection: Upgrade,Keep-Alive");

            //Add cookies if any...
            if (client.Cookies.Count > 0)
            {
                var csb = new StringBuilder();
                for (var i = 0; i < client.Cookies.Count;i++)
                {
                    var cookie = client.Cookies[i];
                    if (i + 1 == client.Cookies.Count)
                        csb.Append(string.Format("{0}={1}",cookie.Name,cookie.Value));
                    else
                        csb.Append(string.Format("{0}={1}; ", cookie.Name, cookie.Value));
                }
                this.Handshake.Add("COOKIE", string.Format("Cookie: {0}",csb.ToString()));
            }            

            this.Handshake.Add("SEC-WEBSOCKET-KEY", "Sec-WebSocket-Key: {0}");
            this.Handshake.Add("SEC-WEBSOCKET-VERSION", "Sec-WebSocket-Version: 13");
            this.Handshake.Add("SEC-WEBSOCKET-PROTOCOL", "Sec-WebSocket-Protocol: XSocketsNET");

            var uri = new Uri(url);
            this._origin = origin;
            this._host = string.Format("{0}:{1}", uri.Host, uri.Port);
           
            this._path = uri.PathAndQuery;
              
            this.Key = GenerateKey();
        }

        private string GenerateKey()
        {
            var bytes = new byte[16];
            var random = new Random();
            for (var index = 0; index < bytes.Length; index++)
            {
                bytes[index] = (byte)random.Next(0, 255);
            }
            return Convert.ToBase64String(bytes);
        }
       
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(string.Format(this.Handshake["GET"]+"\r\n", _path));
            sb.Append(string.Format(this.Handshake["HOST"] + "\r\n", _host));
            sb.Append(string.Format(this.Handshake["ORIGIN"] + "\r\n", _origin));
            sb.Append(this.Handshake["UPGRADE"] + "\r\n");
            sb.Append(this.Handshake["CONNECTION"] + "\r\n");
            for (var i = 0; i < _client.Headers.Count; i++)
            {
                var header = _client.Headers.GetKey(i);
                sb.Append(string.Format("{0}: {1}\r\n",header, _client.Headers.Get(header)));
            }
            if(!string.IsNullOrEmpty(this.Handshake.Get("COOKIE")))
                sb.Append(this.Handshake["COOKIE"] + "\r\n");
            sb.Append(string.Format(this.Handshake["SEC-WEBSOCKET-KEY"] + "\r\n", Key));
            sb.Append(this.Handshake["SEC-WEBSOCKET-VERSION"] + "\r\n");
            sb.Append(this.Handshake["SEC-WEBSOCKET-PROTOCOL"] + "\r\n\r\n");

            return sb.ToString();
            //return string.Format(Handshake, _path, _origin, _host, Key /*, "\r\n" + @"^n:ds[4U"*/);
        }
    }
}