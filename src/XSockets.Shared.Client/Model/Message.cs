
namespace XSockets.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using Common.Event.Arguments;
    using Common.Interfaces;
    using Helpers;

    public class Message : EventArgs, IMessage
    {
        [JsonProperty("I")]
        public int Id { get; set; }
        /// <summary>
        /// Quality Of Service for the message
        /// </summary>
        [JsonProperty("Q")]
        public QoS QoS { get; set; }

        /// <summary>
        /// If true the server will store it as the latest good value to be retained by clients starting a subscription on the topic
        /// </summary>
        [JsonProperty("R")]
        public bool Retain { get; set; }
        
        [JsonProperty("B")]
        public IEnumerable<byte> Blob { get; private set; }

        [JsonProperty("D")]
        public string Data { get; private set; }
        public MessageType MessageType { get; private set; }       
         
        [JsonProperty("C", Required = Required.Always)]
        public string Controller { get; set; }

        [JsonProperty("T", Required = Required.Always)]
        public string Topic { get; set; }
        private IXSocketJsonSerializer _serializer;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Extract<T>()
        {
            if (this.MessageType == MessageType.Binary)
            {
                var m = this.serializer.DeserializeFromString<Message>(this.Data);
                return this.serializer.DeserializeFromString<T>(m.Data);
            }

            //if (typeof (T).IsBuiltIn())
            //    return (dynamic)this.Data;
            
            return this.serializer.DeserializeFromString<T>(this.Data);
        }

        private IXSocketJsonSerializer serializer
        {
            get
            {
                if (this._serializer == null)
                    this._serializer = new XSocketJsonSerializer();
                return this._serializer;
            }
        }

        /// <summary>
        /// For newtonsoft.json
        /// </summary>
        public Message(){}

        public Message(IList<byte> blob, MessageType messageType, string controller = "")
        {
            if (messageType == MessageType.Text)
            {
                var data = this.serializer.DeserializeFromString<Message>(Encoding.UTF8.GetString(blob.ToArray()));
                this.Controller = data.Controller.ToLower();
                if (string.IsNullOrEmpty(this.Controller))
                    this.Controller = controller.ToLower();
                this.Topic = data.Topic.ToLower();
                this.Data = data.Data;
                this.MessageType = messageType;
                this.Blob = null;
                this.Id = data.Id;
                this.QoS = data.QoS;
                this.Retain = data.Retain;
            }
            else if (messageType == MessageType.Binary)
            {
                var rawHeader = blob.Take(8).ToArray();
                var payloadLength = (int)BitConverter.ToInt64(rawHeader, 0);
                var bufferLength = blob.Count - payloadLength;
                this.Blob = blob.Skip(payloadLength + 8).Take(bufferLength).ToList();
                this.Data = Encoding.UTF8.GetString(blob.Skip(8).Take(payloadLength).ToArray());
                var eventInfo = this.serializer.DeserializeFromString<Message>(this.Data);
                this.Controller = eventInfo.Controller.ToLower();
                if (string.IsNullOrEmpty(this.Controller))
                    this.Controller = controller.ToLower();
                this.Topic = eventInfo.Topic.ToLower();
                this.MessageType = messageType;
                this.Id = eventInfo.Id;
                this.QoS = eventInfo.QoS;
                this.Retain = eventInfo.Retain;
            }
            else if (messageType == MessageType.Ping)
            {
                this.Blob = blob;
                this.MessageType = messageType;                
            }
            else if (messageType == MessageType.Pong)
            {
                this.Blob = blob;
                this.MessageType = messageType;
            }
        }        
        
        /// <summary>
        /// Ctor for Blob + Object
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="obj"></param>
        /// <param name="topic"></param>
        /// <param name="controller"></param>
        public Message(IEnumerable<byte> blob, object obj, string topic, string controller = "") : this(blob.ToList(), obj, topic, controller, new XSocketJsonSerializer()) { }

        public Message(IList<byte> blob, object obj, string topic, string controller, IXSocketJsonSerializer serializer)
        {
            this._serializer = serializer;

            //Transform metadata into JSON
            var jsonMeta = this.serializer.SerializeToString(obj);

            ////Add metadata to a TextArgs object
            this.Data = this.serializer.SerializeToString(new Message(jsonMeta, topic, controller));

            ////Set the metadata as header in the binary message
            var ms = new List<byte>();            
            ms.AddRange(blob);

            this.Blob = ms;
            this.Topic = topic.ToLower();
            this.MessageType = MessageType.Binary;

            this.Controller = controller.ToLower();
        }

        public Message(IList<byte> blob, string topic, string controller):this(blob,topic,controller,new XSocketJsonSerializer()){}
        public Message(IList<byte> blob, string topic, string controller, IXSocketJsonSerializer serializer)
        {
            this._serializer = serializer;

            //Transform metadata into JSON
            var jsonMeta = this.serializer.SerializeToString(string.Empty);

            ////Add metadata to a TextArgs object
            this.Data = this.serializer.SerializeToString(new Message(jsonMeta, topic, controller));

            ////Set the metadata as header in the binary message
            var ms = new List<byte>();
            ms.AddRange(blob);

            this.Blob = ms;

            this.Controller = controller.ToLower();
            this.Topic = topic.ToLower();
            this.MessageType = MessageType.Binary;
        }

        /// <summary>
        /// Ctor for Object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="topic"></param>
        /// <param name="controller"></param>                
        public Message(object obj, string topic, string controller) : this(obj, topic, controller, new XSocketJsonSerializer()) { }

        //public Message(object obj, string topic, string controller, IXSocketJsonSerializer serializer)
        //{
        //    this._serializer = serializer;
        //    this.Data = _serializer.SerializeToString(obj);
        //    this.Topic = topic.ToLower();
        //    this.Controller = controller.ToLower();
        //    this.MessageType = MessageType.Text;
        //}

        //public Message(string json, string topic, string controller)
        //{
        //    this.Blob = null;
        //    this.Data = json;
        //    this.Topic = topic.ToLower();
        //    this.Controller = controller.ToLower();
        //    this.MessageType = MessageType.Text;
        //}

        /// <summary>
        /// Ctor for object based message
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="topic"></param>
        /// <param name="controller"></param>
        /// <param name="serializer"></param>
        public Message(object obj, string topic, string controller, IXSocketJsonSerializer serializer)
        {
            _serializer = serializer;
            if (obj is string && !serializer.IsValidJson((string)obj))
                Data = (string)obj;
            else
                Data = serializer.SerializeToString(obj);
            Topic = topic.ToLower();
            Controller = controller.ToLower();
            MessageType = MessageType.Text;
        }

        /// <summary>
        /// Ctor for string based message
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="topic"></param>
        /// <param name="controller"></param>
        /// <param name="isJson"></param>
        public Message(string obj, string topic, string controller = "")
        {
            Blob = null;

            Data = serializer.IsValidJson(obj) ? obj : serializer.SerializeToString(obj);
            //Data = serializer.SerializeToString(obj);
            Topic = topic.ToLower();
            Controller = controller.ToLower();
            MessageType = MessageType.Text;
        }

        public override string ToString()
        {
            switch (this.MessageType)
            {
                case MessageType.Text:
                    return this.serializer.SerializeToString(this);
                    
                case MessageType.Binary:
                    return Encoding.UTF8.GetString(this.getBytes());
                    
                default:
                    throw new Exception("MessageType unknown, this code should never execute");
            }
        }

        public byte[] ToBytes()
        {
            if (this.MessageType == MessageType.Text)
                return Encoding.UTF8.GetBytes(this.serializer.SerializeToString(this));
            return this.getBytes();
        }

        private byte[] getBytes()
        {
            if (this.Data == null)
                return this.Blob.ToArray();
            var ms = new List<byte>();
            var header = BitConverter.GetBytes((Int64)this.Data.Length);
            ms.AddRange(header);
            ms.AddRange(Encoding.UTF8.GetBytes(this.Data));
            ms.AddRange(this.Blob);

            return ms.ToArray();
        }
    }
}