using System;
using System.Text;
using Json.NETMF;
using Microsoft.SPOT;
using XSockets.ClientMF43.Event.Arguments;
using XSockets.ClientMF43.Event.Arguments.Interfaces;

namespace XSockets.ClientMF43.Model
{
    public class Message : EventArgs, IMessage
    {
        public string D { get; private set; }
        public MessageType MessageType { get; private set; }
        public string C { get; set; }
        public string T { get; set; }
        private JsonSerializer _serializer;

        private JsonSerializer serializer
        {
            get
            {
                if (this._serializer == null)
                    this._serializer = new JsonSerializer();
                return this._serializer;
            }
        }

        /// <summary>
        /// Ctor for Object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="topic"></param>
        /// <param name="controller"></param>                
        public Message(object obj, string topic, string controller = "") : this(obj, topic, controller, new JsonSerializer()) { }

        public Message(object obj, string topic, string controller, JsonSerializer serializer)
        {
            this._serializer = serializer;
            this.D = _serializer.Serialize(obj);
            this.T = topic.ToLower();
            this.C = controller;
            this.MessageType = MessageType.Text;
        }

        public Message(string json, string topic, string controller = "")
        {
            this.D = json;
            this.T = topic.ToLower();
            this.C = controller;
            this.MessageType = MessageType.Text;
        }

        public override string ToString()
        {
            switch (this.MessageType)
            {
                case MessageType.Text:
                    return this.serializer.Serialize(this);
                    break;
                    //case MessageType.Binary:
                    //    return Encoding.UTF8.GetString(this.getBytes());
                    break;
                default:
                    throw new Exception("MessageType unknown, this code should never execute");
            }
        }

        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(this.serializer.Serialize(this));
        }
    }
}
