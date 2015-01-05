using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using XSockets.ClientPortableW8.Common.Event.Arguments;
using XSockets.ClientPortableW8.Common.Interfaces;
using XSockets.ClientPortableW8.Globals;
using XSockets.ClientPortableW8.Model;
using XSockets.ClientPortableW8.Protocol;
using XSockets.ClientPortableW8.Protocol.FrameBuilders;
using XSockets.ClientPortableW8.Utility.Storage;

namespace XSockets.ClientPortableW8
{    
    public partial class Controller : IController
    {
        private static readonly object Locker = new object();
        public IClientInfo ClientInfo { get; set; }
        public IXSocketClient XSocketClient { get; private set; }

        public event EventHandler<Message> OnMessage;
        public event EventHandler<OnClientConnectArgs> OnOpen;
        public event EventHandler<OnClientDisconnectArgs> OnClose;
        public event EventHandler<OnErrorArgs> OnError;
        

        private readonly RepositoryInstance<string, ISubscription> _subscriptions;
        private readonly RepositoryInstance<string, IListener> _listeners;

        private readonly List<byte> _queuedFrames; 
        
        public Controller(IXSocketClient client, string controller)
        {
            this._queuedFrames = new List<byte>();
            this._subscriptions = new RepositoryInstance<string, ISubscription>();
            this._listeners = new RepositoryInstance<string, IListener>();
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
                var binding = this._subscriptions.GetById(@event);
                var listener = this._listeners.GetById(@event);

                if (binding == null)
                    binding = this._subscriptions.GetById(message.Topic);

                if (listener == null)
                    listener = this._listeners.GetById(message.Topic);

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
                var binding = this._subscriptions.GetById(@event);
                var listener = this._listeners.GetById(@event);

                if (binding == null)
                    binding = this._subscriptions.GetById(message.Topic);

                if (listener == null)
                    listener = this._listeners.GetById(message.Topic);

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

        private async void FireBoundMethod(ISubscription binding, IMessage message)
        {
            if (binding.Callback != null)
                binding.Callback(message);
            
            binding.Counter++;
            if (binding.SubscriptionType == SubscriptionType.One)
                await this.Unsubscribe(binding.Topic);
            else if (binding.SubscriptionType == SubscriptionType.Many && binding.Counter == binding.Limit)
            {
                await this.Unsubscribe(binding.Topic);
            }
        }

        private void FireBoundMethod(IListener listener, IMessage message)
        {
            if (listener.Callback != null)
                listener.Callback(message);
            
            listener.Counter++;
            if (listener.SubscriptionType == SubscriptionType.One)
                this._listeners.Remove(listener.Topic);
            else if (listener.SubscriptionType == SubscriptionType.Many && listener.Counter == listener.Limit)
            {
                this._listeners.Remove(listener.Topic);
            }
        }

        internal async void OpenController()
        {
            var m = new Message(new { Init = true }, Constants.Events.Controller.Init, this.ClientInfo.Controller);
            var f = GetDataFrame(m).ToBytes();
            await this.XSocketClient.Communication.SendAsync(f);
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

        private async void FireOpened()
        {
            if (this.OnOpen != null)
                this.OnOpen.Invoke(this, new OnClientConnectArgs(this.ClientInfo));

            foreach (var subscription in this._subscriptions.GetAll())
            {
                if(subscription.Topic == Constants.Events.Error || subscription.Topic == Constants.Events.Controller.Closed || subscription.Topic == Constants.Events.Controller.Opened)continue;
                var payload = new Message(new XSubscription { Topic = subscription.Topic, Ack = subscription.Confirm, Controller = this.ClientInfo.Controller }, Constants.Events.PubSub.Subscribe,this.ClientInfo.Controller);                
                var frame = GetDataFrame(payload).ToBytes();
                this._queuedFrames.AddRange(frame);
            }
            await this.XSocketClient.Communication.SendAsync(_queuedFrames.ToArray());             
            this._queuedFrames.Clear();
        }
        public void FireClosed()
        {
            lock(Locker){
               if (this.ClientInfo.ConnectionId == Guid.Empty) return;         
                
                if (this.OnClose != null)
                    this.OnClose.Invoke(this, new OnClientDisconnectArgs(this.ClientInfo));
                this.ClientInfo.ConnectionId = Guid.Empty;                
            }
        }
        public async virtual void Close()
        {
            try
            {
                await this.Invoke(Constants.Events.Controller.Closed);
            }
            catch
            {             
            }
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
            return GetDataFrame(FrameType.Binary, message.ToBytes());
        }


        public virtual async Task SetEnum(string propertyName, string value)
        {
            await this.Invoke("set_" + propertyName,value);
        }

        public virtual async Task SetProperty(string propertyName, object value)
        {
            if(IsBuiltIn(value.GetType()))
                await this.Invoke("set_" + propertyName,new {value});
            else
            {
                await this.Invoke("set_" + propertyName, value);
            }
        }

        private static bool IsBuiltIn(Type type)
        {            
            //TODO: Have better check here
            
            if (type.Namespace != null && (type.Namespace.StartsWith("System")))// && (type.Module.ScopeName == "CommonLanguageRuntimeLibrary" || type.Module.ScopeName == "mscorlib.dll")))
            {
                return true;
            }
            return false;
        }
    }
}