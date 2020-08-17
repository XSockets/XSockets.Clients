
namespace XSockets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.Interfaces;
    using Globals;
    using Model;

    public partial class Controller : IController
    {        
        private void AddDefaultSubscriptions()
        {
            var onError = new Subscription(Constants.Events.Error, Error) { IsBound = true };
            this._subscriptions.AddOrUpdate(Constants.Events.Error, onError);
            var onOpen = new Subscription(Constants.Events.Controller.Opened, Opened) { IsBound = true };
            this._subscriptions.AddOrUpdate(Constants.Events.Controller.Opened, onOpen);
            var onClose = new Subscription(Constants.Events.Controller.Closed, Closed) { IsBound = true };
            this._subscriptions.AddOrUpdate(Constants.Events.Controller.Closed, onClose);
        }

        //private void ResetSubscriptions()
        //{
        //    foreach (var s in this._subscriptions.GetAllWithKeys())
        //    {
        //        if(s.Key == Constants.Events.Controller.Closed || s.Key == Constants.Events.Controller.Opened || s.Key == Constants.Events.Error) continue;

        //        s.Value.IsBound = false;
        //        this._subscriptions.AddOrUpdate(s.Key, s.Value);
        //    }
        //}

        public virtual void BindUnboundSubscriptions()
        {
            var unboundBindings = this._subscriptions.Find(p => p.IsBound == false).ToList();

            if (!unboundBindings.Any()) return;
            foreach (var unboundBinding in unboundBindings)
            {
                var binding = unboundBinding;
                new Task(async
                    () =>
                    await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = binding.Topic.ToLower(), Ack = binding.Confirm }, QoS.FireAndForget, false), () =>
                    {
                        var b = this._subscriptions.GetById(binding.Topic);
                        b.IsBound = true;
                        this._subscriptions.AddOrUpdate(binding.Topic, b);
                    })).RunSynchronously();
            }
        }

        public virtual IMessage AsMessage(string topic, object o, QoS qos, bool retain)
        {
            return new Message(o, topic, this.ClientInfo.Controller) { QoS = qos, Retain = retain };
        }

        public virtual async Task One(string topic, Action<IMessage> callback)
        {
            await this.Subscribe(topic, callback, SubscriptionType.One);
        }

        public virtual async Task One<T>(string topic, Action<T> callback)
        {
            await this.Subscribe(topic, callback, SubscriptionType.One);
        }

        public virtual async Task One(string topic, Action<IMessage> callback, Action<IMessage> confirmCallback)
        {
            await this.Subscribe(topic, callback, confirmCallback, SubscriptionType.One);
        }

        public virtual async Task One<T>(string topic, Action<T> callback, Action<IMessage> confirmCallback)
        {
            await this.Subscribe(topic, callback, confirmCallback, SubscriptionType.One);
        }

        public virtual async Task Many(string topic, uint limit, Action<IMessage> callback)
        {
            await this.Subscribe(topic, callback, SubscriptionType.Many, limit);
        }

        public virtual async Task Many<T>(string topic, uint limit, Action<T> callback)
        {
            await this.Subscribe(topic, callback, SubscriptionType.Many, limit);
        }

        public virtual async Task Many(string topic, uint limit, Action<IMessage> callback, Action<IMessage> confirmCallback)
        {
            await this.Subscribe(topic, callback, confirmCallback, SubscriptionType.Many, limit);
        }

        public virtual async Task Many<T>(string topic, uint limit, Action<T> callback, Action<IMessage> confirmCallback)
        {
            await this.Subscribe(topic, callback, confirmCallback, SubscriptionType.Many, limit);
        }

        private void AddConfirmCallback(Action<IMessage> confirmCallback, string @event)
        {
            var e = $"__{@event}";

            if (@event.Contains("."))
            {
                var info = @event.Split('.');
                e = $"{info[0]}.__{info[1]}";
            }
            if (this._subscriptions.ContainsKey(e)) return;

            var confirm = new Subscription(e, confirmCallback);
            confirm.IsBound = this.XSocketClient.IsConnected;
            this._subscriptions.AddOrUpdate(e, confirm);
        }

        /// <summary>
        /// Remove the subscription from the list
        /// </summary>
        /// <param name="topic"></param>
        public virtual async Task Unsubscribe(string topic)
        {
            topic = topic.ToLower();
            ISubscription subscription = this._subscriptions.GetById(topic);

            if (subscription == null)
            {
                subscription = this._subscriptions.GetById(topic);
            }

            if (subscription == null) return;

            if (this.XSocketClient.IsConnected)
            {
                //TODO: fix so that we expect unsuback instead
                await Publish(this.AsMessage(Constants.Events.PubSub.Unsubscribe, new XSubscription { Topic = topic }, QoS.FireAndForget, false));
            }
            this._subscriptions.Remove(topic);
        }

        public virtual async Task Subscribe(string topic)
        {
            await this.Subscribe(topic, SubscriptionType.All);
        }

        public virtual async Task Subscribe(string topic, SubscriptionType subscriptionType, uint limit = 0)
        {
            var subscription = new Subscription(topic, subscriptionType, limit);
            this._subscriptions.AddOrUpdate(topic, subscription);

            if (this.XSocketClient.IsConnected)
            {
                //TODO: fix suback instead of FireAndForget
                await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription
                {
                    Topic = subscription.Topic
                }, QoS.FireAndForget, false), () => { subscription.IsBound = true; });
            }
        }
        public virtual async Task Subscribe(string topic, Action<IMessage> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0)
        {
            var subscription = new Subscription(topic, callback, subscriptionType, limit);
            this._subscriptions.AddOrUpdate(topic, subscription);

            if (this.XSocketClient.IsConnected)
            {
                //TODO: fix suback instead of FireAndForget
                await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription
                {
                    Topic = subscription.Topic
                }, QoS.FireAndForget, false), () => { subscription.IsBound = true; });
            }
        }
        public virtual async Task Subscribe(string topic, Action<IMessage> callback, Action<IMessage> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0)
        {
            var subscription = new Subscription(topic, callback, subscriptionType, limit, true);
            this._subscriptions.AddOrUpdate(topic, subscription);

            AddConfirmCallback(confirmCallback, topic);
            if (this.XSocketClient.IsConnected)
            {
                //TODO: fix suback instead of FireAndForget
                await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = subscription.Topic, Ack = true, Controller = this.ClientInfo.Controller },QoS.FireAndForget, false), () => subscription.IsBound = true);
            }
        }

        public virtual async Task Subscribe<T>(string topic, Action<T> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0) //where T : class
        {
            var subscription = new Subscription(topic, message => callback(this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data)), subscriptionType, limit);
            
            this._subscriptions.AddOrUpdate(topic, subscription);
            if (this.XSocketClient.IsConnected)
            {
                //TODO: fix suback instead of FireAndForget
                await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = subscription.Topic, Ack = false, Controller = this.ClientInfo.Controller }, QoS.FireAndForget, false), () => subscription.IsBound = true);
            }
        }

        public virtual async Task Subscribe<T>(string topic, Action<T> callback, Action<IMessage> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0) //where T : class
        {            
            var subscription = new Subscription(topic, message => callback(this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data)), subscriptionType, limit);            
            
            this._subscriptions.AddOrUpdate(topic, subscription);
            AddConfirmCallback(confirmCallback, topic);
            if (this.XSocketClient.IsConnected)
            {
                //TODO: fix suback instead of FireAndForget
                await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = subscription.Topic, Ack = true, Controller = this.ClientInfo.Controller }, QoS.FireAndForget, false), () => subscription.IsBound = true);
            }
        }
        //Sending methods

        /// <summary>
        ///     Send message
        /// </summary>
        /// <param name="payload">IMessage</param>
        /// <param name="callback"> </param>
        public virtual async Task Publish(IMessage payload, Action callback)
        {
            if (!this.XSocketClient.IsConnected)
                throw new Exception("You cant send messages when not connected to the server");
            
            //TODO: Handle QoS and store messages with QoS > 0

            payload.Controller = this.ClientInfo.Controller;

            await this.SendAsync(payload);
        }

        //public virtual async Task Publish(string payload, Action callback, QoS qos = QoS.FireAndForget)
        //{
        //    if (!this.XSocketClient.IsConnected)
        //        throw new Exception("You cant send messages when not connected to the server");



        //    var frame = GetDataFrame(payload).ToBytes();
        //    //If controller not yet open... Queue message
        //    if (this.ClientInfo.ConnectionId == Guid.Empty)
        //    {
        //        this._queuedFrames.AddRange(frame);
        //        return;
        //    }
        //    await this.XSocketClient.Communication.SendAsync(frame, () => { });
        //}

        public virtual async Task Publish(IMessage payload, bool isAck = false)
        {
            if (!this.XSocketClient.IsConnected)
                throw new Exception("You cant send messages when not connected to the server");
            
            payload.Controller = this.ClientInfo.Controller;
            await this.SendAsync(payload, isAck);
        }

        internal async Task SendAsync(IMessage m, bool isAck = false)
        {
            //Handle QoS            
            if(!isAck && m.QoS > QoS.FireAndForget)
            {
                m.Id = this.GetIdForQoS();
                System.Diagnostics.Debug.WriteLine(this.XSocketClient.Serializer.SerializeToString(m));
                this.XSocketClient.QoSRepository.AddOrUpdate(m.Id, GetDataFrame(m).ToBytes());
            }

            var frame = GetDataFrame(m).ToBytes();
            // TODO : This can be correct... SHould be able to send message or the controller will never be opened
            //If controller not yet open... Queue message
            //if (this.ClientInfo.ConnectionId == Guid.Empty)
            if(!this.XSocketClient.IsConnected)
            {
                this._queuedFrames.AddRange(frame);
                return;
            }

            await this.XSocketClient.Communication.SendAsync(frame, () => { });
        }

        public virtual async Task Publish(string topic, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Publish(this.AsMessage(topic,null,qos, retain));
        }

        public virtual async Task Publish(string topic, object obj, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Publish(this.AsMessage(topic, obj, qos, retain));
        }

        public virtual async Task Publish(string topic, object obj, Action callback, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Publish(this.AsMessage(topic, obj, qos, retain), callback);
        }

        public virtual async Task Publish(string topic, byte[] data, object metadata, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Publish(topic, data.ToList(), metadata, qos, retain);
        }
        public virtual async Task Publish(string topic, List<byte> data, object metadata, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Publish(new Message(data, metadata, topic, this.ClientInfo.Controller) {QoS = qos, Retain = retain });
        }
        public virtual async Task Publish(string topic, byte[] data, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Publish(topic, data.ToList(),qos, retain);
        }
        public virtual async Task Publish(string topic, List<byte> data, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Publish(new Message(data, topic, this.ClientInfo.Controller) { QoS = qos, Retain = retain });
        }        
    }    
}