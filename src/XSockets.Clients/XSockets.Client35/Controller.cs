using System;
using System.Collections.Generic;
using System.Text;
using XSockets.Client35.Common.Event.Arguments;
using XSockets.Client35.Common.Interfaces;
using XSockets.Client35.Globals;
using XSockets.Client35.Model;
using XSockets.Client35.Protocol;
using XSockets.Client35.Protocol.FrameBuilders;
using XSockets.Client35.Utility.Storage;

namespace XSockets.Client35
{    
    public partial class Controller : IController
    {
        private static object locker = new object();
        public IClientInfo ClientInfo { get; set; }
        public IXSocketClient XSocketClient { get; private set; }

        public event EventHandler<Message> OnMessage;
        public event EventHandler<OnClientConnectArgs> OnOpen;
        public event EventHandler<OnClientDisconnectArgs> OnClose;
        public event EventHandler<OnErrorArgs> OnError;
        

        private RepositoryInstance<string, ISubscription> Subscriptions;
        private RepositoryInstance<string, IListener> Listeners;

        private List<byte> queuedFrames; 
        
        public Controller(IXSocketClient client, string controller)
        {
            this.queuedFrames = new List<byte>();
            this.Subscriptions = new RepositoryInstance<string, ISubscription>();
            this.Listeners = new RepositoryInstance<string, IListener>();
            this.AddDefaultSubscriptions();
            this.ClientInfo = new ClientInfo{PersistentId = client.PersistentId, Controller = controller};
            this.XSocketClient = client;
        }

        public virtual void FireOnBlob(IMessage message)
        {
            try
            {
                var @event = (message.Controller == null) ? message.Topic : message.Controller + "." + message.Topic;

                var fired = false;

                //PUB/SUB & RPC
                var binding = this.Subscriptions.GetById(@event);
                var listener = this.Listeners.GetById(@event);

                if (binding == null)
                    binding = this.Subscriptions.GetById(message.Topic);

                if (listener == null)
                    listener = this.Listeners.GetById(message.Topic);

                if (binding != null)
                {
                    this.FireBoundMethod(binding, message);
                    fired = true;
                }
                if (listener != null)
                {
                    this.FireBoundMethod(listener, message);
                    fired = true;
                }

                if (fired) return;

                //fire onmessage since there was no binding
                if (this.OnMessage != null) this.OnMessage.Invoke(this, message as Message);

            }
            catch(Exception ex)
            {
                if (this.OnError != null) this.OnError.Invoke(this, new OnErrorArgs(ex));
            }
        }

        /// <summary>
        /// A message was received
        /// </summary>
        /// <param name="message"></param>
        public virtual void FireOnMessage(IMessage message)
        {
            try
            {
                var @event = (message.Controller == null) ? message.Topic : message.Controller + "." + message.Topic;

                var fired = false;

                //PUB/SUB & RPC
                var binding = this.Subscriptions.GetById(@event);
                var listener = this.Listeners.GetById(@event);

                if (binding == null)
                    binding = this.Subscriptions.GetById(message.Topic);

                if (listener == null)
                    listener = this.Listeners.GetById(message.Topic);

                if (binding != null)
                {
                    this.FireBoundMethod(binding, message);
                    fired = true;
                }
                if (listener != null)
                {
                    this.FireBoundMethod(listener, message);
                    fired = true;
                }

                if (fired) return;

                //fire onmessage since there was no binding
                if (this.OnMessage != null) this.OnMessage.Invoke(this, message as Message);
            }
            catch (Exception ex) // Will dispatch to OnError on exception
            {
                if (this.OnError != null) this.OnError.Invoke(this, new OnErrorArgs(ex));
            }
        }

        private void FireBoundMethod(ISubscription binding, IMessage message)
        {
            if (binding.Callback == null)
            {
                if (message.MessageType == MessageType.Text)
                {                   
                    binding.Execute(this.XSocketClient.Serializer.DeserializeFromString(message.Data, binding.Type));                   
                }
                else
                {
                    binding.Execute(message);
                }
            }
            else
            {
                binding.Callback(message);
            }
            binding.Counter++;
            if (binding.SubscriptionType == SubscriptionType.One)
                this.Unsubscribe(binding.Topic);
            else if (binding.SubscriptionType == SubscriptionType.Many && binding.Counter == binding.Limit)
            {
                this.Unsubscribe(binding.Topic);
            }
        }

        private void FireBoundMethod(IListener listener, IMessage message)
        {
            if (listener.Callback == null)
            {
                if (message.MessageType == MessageType.Text)
                {
                    if (listener.IsStorageObject)
                    {
                        var xs = this.XSocketClient.Serializer.DeserializeFromString<XStorage>(message.Data);
                        if (xs.Value == null)
                            listener.Execute(Activator.CreateInstance(listener.Type));
                        else
                            listener.Execute(this.XSocketClient.Serializer.DeserializeFromString(xs.Value.ToString(), listener.Type));
                    }
                    else
                    {
                        listener.Execute(this.XSocketClient.Serializer.DeserializeFromString(message.Data, listener.Type));
                    }
                }
                else
                {
                    listener.Execute(message);
                }
            }
            else
            {
                listener.Callback(message);
            }
            listener.Counter++;
            if (listener.SubscriptionType == SubscriptionType.One)
                this.Listeners.Remove(listener.Topic);
            else if (listener.SubscriptionType == SubscriptionType.Many && listener.Counter == listener.Limit)
            {
                this.Listeners.Remove(listener.Topic);
            }
        }

        internal void OpenController()
        {
            var m = new Message(new { Init = true }, Constants.Events.Controller.Init, this.ClientInfo.Controller);
            var f = GetDataFrame(m).ToBytes();
            this.XSocketClient.Socket.Send(f, () => { }, err => FireClosed());
        }

        private void Opened(IMessage message)
        {
            this.ClientInfo = this.XSocketClient.Serializer.DeserializeFromString<ClientInfo>(message.Data);
            this.ClientInfo.Controller = message.Controller;
            this.XSocketClient.PersistentId = this.ClientInfo.PersistentId;
            FireOpened();
        }

        private void Closed(IMessage message)
        {
            
            FireClosed();
        }

        private void Error(IMessage error)
        {
            var err = new OnErrorArgs(this.XSocketClient.Serializer.DeserializeFromString<Exception>(error.Data));
            this.Error(err);
        }

        private void Error(OnErrorArgs args)
        {
            if (this.OnError != null)
                this.OnError.Invoke(this, args);
            this.XSocketClient.FireError(args);
        }

        private void FireOpened()
        {
            if (this.OnOpen != null)
                this.OnOpen.Invoke(this, new OnClientConnectArgs(this.ClientInfo));

            this.XSocketClient.Socket.Send(queuedFrames.ToArray(), () => { }, err => FireClosed());             
            this.queuedFrames.Clear();
        }
        private void FireClosed()
        {
            lock(locker){
               if (this.ClientInfo.ConnectionId == Guid.Empty) return;
         
                
                if (this.OnClose != null)
                    this.OnClose.Invoke(this, new OnClientDisconnectArgs(this.ClientInfo));
                this.ClientInfo.ConnectionId = Guid.Empty;
                this.XSocketClient.Controllers.Remove(this.ClientInfo.Controller);
            }
        }
        public void Close()
        {         
            this.Invoke(Globals.Constants.Events.Controller.Closed);            
        }        

        private Rfc6455DataFrame GetDataFrame(FrameType frameType, byte[] payload)
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

        private Rfc6455DataFrame GetDataFrame(IMessage message)
        {
            if (message.MessageType == MessageType.Text)
                return GetDataFrame(FrameType.Text, Encoding.UTF8.GetBytes(message.ToString()));
            return GetDataFrame(FrameType.Binary, message.ToBytes());// Encoding.UTF8.GetBytes(message.ToString())); //return GetDataFrame(FrameType.Binary, message.Blob.ToArray());
        }


        public void SetEnum(string propertyName, string value)
        {
            this.Invoke("set_" + propertyName,value);
        }

        public void SetProperty(string propertyName, object value)
        {
            if(IsBuiltIn(value.GetType()))
                this.Invoke("set_" + propertyName,new {value= value});
            else
            {
                this.Invoke("set_" + propertyName, value);
            }
        }

        private static bool IsBuiltIn(Type type)
        {
            if (type.Namespace != null && (type.Namespace.StartsWith("System") && (type.Module.ScopeName == "CommonLanguageRuntimeLibrary" || type.Module.ScopeName == "mscorlib.dll")))
            {
                return true;
            }
            return false;
        }
    }
}