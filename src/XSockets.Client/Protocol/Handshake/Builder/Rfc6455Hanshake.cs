﻿
namespace XSockets.Protocol.Handshake.Builder
{
    using System;
    using System.Net;
    using System.Text;
    using XSockets.Common.Interfaces;
    using XSockets.Helpers;

    internal class Rfc6455Handshake
    {
        public NameValueCollection Handshake { get; set; }

        private IXSocketClient _client;        

        private readonly string _host = string.Empty;
        private readonly string _origin = string.Empty;
        private readonly string _path = string.Empty;
        private string Key { get; set; }

        public Rfc6455Handshake(string url, string origin, IXSocketClient client)
        {
            _client = client;
            this.Handshake = new NameValueCollection();
            this.Handshake.Add("GET", "GET {0} HTTP/1.1");
            this.Handshake.Add("HOST", "Host: {0}");
            this.Handshake.Add("ORIGIN", "Origin: {0}");
            this.Handshake.Add("UPGRADE", "Upgrade: websocket");
            this.Handshake.Add("CONNECTION", "Connection: Upgrade,Keep-Alive");

            //Add cookies if any...
            if (client.Cookies.Count > 0)
            {
                var csb = new StringBuilder();
                var ii = 0;
                foreach (Cookie cookie in client.Cookies)
                {                    
                    if (ii + 1 == client.Cookies.Count)
                        csb.Append($"{cookie.Name}={cookie.Value}");
                    else
                        csb.Append($"{cookie.Name}={cookie.Value}; ");
                    ii++;
                }
                
                this.Handshake.Add("COOKIE", $"Cookie: {csb}");
            }

            this.Handshake.Add("SEC-WEBSOCKET-KEY", "Sec-WebSocket-Key: {0}");
            this.Handshake.Add("SEC-WEBSOCKET-VERSION", "Sec-WebSocket-Version: 13");
            this.Handshake.Add("SEC-WEBSOCKET-PROTOCOL", "Sec-WebSocket-Protocol: XSocketsNET");

            var uri = new Uri(url);
            this._origin = origin;
            this._host = $"{uri.Host}:{uri.Port}";

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
            sb.Append(string.Format(this.Handshake["GET"] + "\r\n", _path));
            sb.Append(string.Format(this.Handshake["HOST"] + "\r\n", _host));
            sb.Append(string.Format(this.Handshake["ORIGIN"] + "\r\n", _origin));
            sb.Append(this.Handshake["UPGRADE"] + "\r\n");
            sb.Append(this.Handshake["CONNECTION"] + "\r\n");
            foreach (var header in _client.Headers)
            {
                sb.Append($"{header.Key}: {header.Value}\r\n");
            }           
            if (this.Handshake.ContainsKey("COOKIE"))
                sb.Append(this.Handshake["COOKIE"] + "\r\n");
            sb.Append(string.Format(this.Handshake["SEC-WEBSOCKET-KEY"] + "\r\n", Key));
            sb.Append(this.Handshake["SEC-WEBSOCKET-VERSION"] + "\r\n");
            sb.Append(this.Handshake["SEC-WEBSOCKET-PROTOCOL"] + "\r\n\r\n");

            return sb.ToString();
        }
    }
}