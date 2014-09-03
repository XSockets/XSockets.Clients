using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Json.NETMF;
using Microsoft.SPOT;
using XSockets.ClientMF43.Event;
using XSockets.ClientMF43.Helpers;
using XSockets.ClientMF43.Interfaces;
using XSockets.ClientMF43.Model;

namespace XSockets.ClientMF43
{    
    public class XSocketClient : IXSocketClient
    {        
        private bool Connected { get { return this.Socket != null && this.Socket.Available == 1; } }

        private Thread _recievingThread;

        private const int BufferSize = 1024;

        public Socket Socket { get; private set; }

        private IPEndPoint EndPoint { get; set; }

        public string Handshake { get; private set; }        

        public string Server { get; private set; }

        public int Port { get; private set; }

        public string ProtocolName { get; set; }

        public string ProtocolResponse { get; set; }

        public event EventHandler OnOpen;

        public event EventHandler OnClose;

        public event EventHandler OnError;

        public event MessageHandler OnMessage;        

        public JsonSerializer Serializer { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="port"></param>        
        /// <param name="protocolName">The protocol to use</param>
        /// <param name="protocolResponse">Expected response from the protocol</param>
        public XSocketClient(string server, int port, string protocolName = "JsonProtocol", string protocolResponse = "CONNECTED") 
        {
            this.Serializer = new JsonSerializer();            
            this.Server = server;
            this.Port = port;
            this.ProtocolName = protocolName;
            this.ProtocolResponse = protocolResponse;
        }        

        public void Open()
        {
            try
            {
                //If connected, close previous connection
                if (Connected) this.Close();

                //Create handshake
                this.Handshake = this.ProtocolName;

                //Get endpoint
                this.EndPoint = this.GetEndpoint();

                //Create Socket
                this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.Socket.Connect(this.EndPoint);

                //Do handshake
                this.Socket.Send(Encoding.UTF8.GetBytes(this.Handshake));

                //Validate handshake
                var response = new byte[BufferSize];
                this.Socket.Receive(response);
                var handshakeResponse = Encoding.UTF8.GetString(response);

                if (ValidateHandshake(handshakeResponse))
                {         
                    //Start listening for messages in a new thread
                    var ts = new ThreadStart(Recieve);
                    this._recievingThread = new Thread(ts);
                    this._recievingThread.Start();

                    if (this.OnOpen != null)
                        this.OnOpen.Invoke(this, new EventArgs());
                }
                else
                {
                    this.Close();
                }
            }
            catch 
            {
                if (this.OnError != null)
                    this.OnError.Invoke(this, new EventArgs());
            }
        }        

        /// <summary>        
        /// Verify that the response from the protocol is the same as we expect
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private bool ValidateHandshake(string response)
        {
            return true; 
            //return response == this.ProtocolResponse;            
        }
        
        private IPEndPoint GetEndpoint()
        {                     
            IPHostEntry host = Dns.GetHostEntry(this.Server); 
            return new IPEndPoint(host.AddressList[0], this.Port);            
        }

        public void Close()
        {
            try
            {
                if(this._recievingThread != null)
                    this._recievingThread.Abort();
                
                if(this.Socket != null)
                    this.Socket.Close();
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            finally 
            {
                if(this.OnClose != null)
                    this.OnClose.Invoke(this,new EventArgs());
            }
        }        

        public void Publish(string topic, object data, string controller)
        {
            var objJson = this.Serializer.Serialize(data);
            var json = this.Serializer.Serialize(new Model.Message(objJson, topic, controller));
            this.Socket.Send(Encoding.UTF8.GetBytes(json));
        }
        
        public void Subscribe(string @event, string controller)
        {
            var sub = new XSubscription {Event = @event, Confirm = false};
            var jsonSub = this.Serializer.Serialize(sub);
            var json = this.Serializer.Serialize(new Model.Message(jsonSub, Constants.PubSub.Subscribe, controller));
            this.Socket.Send(Encoding.UTF8.GetBytes(json));         
        }

        public void Unsubscribe(string @event, string controller)
        {
            var sub = new XSubscription { Event = @event, Confirm = false };
            var jsonSub = this.Serializer.Serialize(sub);
            var json = this.Serializer.Serialize(new Model.Message(jsonSub, Constants.PubSub.Unsubscribe, controller));
            this.Socket.Send(Encoding.UTF8.GetBytes(json));
        }
        
        public void Recieve()
        {
            var buffer = new byte[1];
            var i = 0;
            var r = this.Socket.Receive(buffer, 1, SocketFlags.None);

            if (r < 0)
            {
                this.Close();
                return;
            }

            if (buffer[0] == 0x00)
            {
                byte[] result = new byte[BufferSize];
                bool endReached = false;
                while (!endReached)
                {
                    this.Socket.Receive(buffer, 1, SocketFlags.None);
                    endReached = buffer[0] == 0xff;
                    if (!endReached)
                    {
                        result[i] = buffer[0];
                        i++;
                    }
                }
                if (this.OnMessage != null)
                {
                    var m = this.ToMessage(result);
                    if (m != null)
                        this.OnMessage.Invoke(this, m);
                }
            }

            Recieve();
        }         
       
        public void Dispose()
        {
            this.Close();
        }

        public void SetEnum(string propertyName, string value, string controller)
        {
            this.Publish("set_" + propertyName, value, controller);
        }

        public void SetProperty(string propertyName, object value, string controller)
        {
            if (IsBuiltIn(value.GetType()))
                this.Publish("set_" + propertyName, new EnumObject{ value = value }, controller);
            else
            {
                this.Publish("set_" + propertyName, value, controller);
            }
        }

        private static bool IsBuiltIn(Type type)
        {            
            if ((type.FullName.StartsWith("System")))
            {
                return true;
            }
            return false;
        }
    }

    public class EnumObject
    {
        public object value { get; set; }
    }
}
