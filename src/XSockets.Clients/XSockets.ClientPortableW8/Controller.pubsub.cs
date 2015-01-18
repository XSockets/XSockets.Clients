using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XSockets.ClientPortableW8.Common.Interfaces;
using XSockets.ClientPortableW8.Globals;
using XSockets.ClientPortableW8.Model;

namespace XSockets.ClientPortableW8
{    
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

        private void ResetSubscriptions()
        {
            foreach (var s in this._subscriptions.GetAllWithKeys())
            {
                if(s.Key == Constants.Events.Controller.Closed || s.Key == Constants.Events.Controller.Opened || s.Key == Constants.Events.Error) continue;

                s.Value.IsBound = false;
                this._subscriptions.AddOrUpdate(s.Key, s.Value);
            }
        }

        public virtual void BindUnboundSubscriptions()
        {
            var unboundBindings = this._subscriptions.Find(p => p.IsBound == false).ToList();

            if (!unboundBindings.Any()) return;
            foreach (var unboundBinding in unboundBindings)
            {
                var binding = unboundBinding;
                new Task(
                    () =>
                    Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = binding.Topic.ToLower(), Ack = binding.Confirm }), () =>
                    {
                        var b = this._subscriptions.GetById(binding.Topic);
                        b.IsBound = true;
                        this._subscriptions.AddOrUpdate(binding.Topic, b);
                    })).RunSynchronously();
            }
        }

        public virtual IMessage AsMessage(string topic, object o)
        {
            return new Message(o, topic);
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
            var e = string.Format("__{0}", @event);

            if (@event.Contains("."))
            {
                var info = @event.Split('.');
                e = string.Format("{0}.__{1}", info[0], info[1]);
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
                await Publish(this.AsMessage(Constants.Events.PubSub.Unsubscribe, new XSubscription { Topic = topic }));
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
                await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription
                {
                    Topic = subscription.Topic
                }), () => { subscription.IsBound = true; });
            }
        }
        public virtual async Task Subscribe(string topic, Action<IMessage> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0)
        {
            var subscription = new Subscription(topic, callback, subscriptionType, limit);
            this._subscriptions.AddOrUpdate(topic, subscription);

            if (this.XSocketClient.IsConnected)
            {
                await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription
                {
                    Topic = subscription.Topic
                }), () => { subscription.IsBound = true; });
            }
        }
        public virtual async Task Subscribe(string topic, Action<IMessage> callback, Action<IMessage> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0)
        {
            var subscription = new Subscription(topic, callback, subscriptionType, limit, true);
            this._subscriptions.AddOrUpdate(topic, subscription);

            AddConfirmCallback(confirmCallback, topic);
            if (this.XSocketClient.IsConnected)
            {
                await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = subscription.Topic, Ack = true, Controller = this.ClientInfo.Controller }), () => subscription.IsBound = true);
            }
        }

        public virtual async Task Subscribe<T>(string topic, Action<T> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0) //where T : class
        {
            var subscription = new Subscription(topic, message => callback(this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data)), subscriptionType, limit);
            
            this._subscriptions.AddOrUpdate(topic, subscription);
            if (this.XSocketClient.IsConnected)
            {
                await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = subscription.Topic, Ack = false, Controller = this.ClientInfo.Controller }), () => subscription.IsBound = true);
            }
        }

        public virtual async Task Subscribe<T>(string topic, Action<T> callback, Action<IMessage> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0) //where T : class
        {            
            var subscription = new Subscription(topic, message => callback(this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data)), subscriptionType, limit);            
            
            this._subscriptions.AddOrUpdate(topic, subscription);
            AddConfirmCallback(confirmCallback, topic);
            if (this.XSocketClient.IsConnected)
            {
                await Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = subscription.Topic, Ack = true, Controller = this.ClientInfo.Controller }), () => subscription.IsBound = true);
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
            
            payload.Controller = this.ClientInfo.Controller;
            var frame = GetDataFrame(payload).ToBytes();
            //If controller not yet open... Queue message
            if (this.ClientInfo.ConnectionId == Guid.Empty)
            {
                this._queuedFrames.AddRange(frame);
                return;
            }

            await this.XSocketClient.Communication.SendAsync(frame, () => { });
        }

        public virtual async Task Publish(string payload, Action callback)
        {
            if (!this.XSocketClient.IsConnected)
                throw new Exception("You cant send messages when not connected to the server");

            var frame = GetDataFrame(payload).ToBytes();
            //If controller not yet open... Queue message
            if (this.ClientInfo.ConnectionId == Guid.Empty)
            {
                this._queuedFrames.AddRange(frame);
                return;
            }
            await this.XSocketClient.Communication.SendAsync(frame, () => { });
        }

        public virtual async Task Publish(IMessage payload)
        {
            if (!this.XSocketClient.IsConnected)
                throw new Exception("You cant send messages when not connected to the server");
            
            payload.Controller = this.ClientInfo.Controller;
            var frame = GetDataFrame(payload).ToBytes();
            //If controller not yet open... Queue message
            if (this.ClientInfo.ConnectionId == Guid.Empty)
            {
                this._queuedFrames.AddRange(frame);
                return;
            }

            await this.XSocketClient.Communication.SendAsync(frame, () => { });
        }

        public virtual async Task Publish(string topic)
        {
            await this.Publish(this.AsMessage(topic,null));
        }

        public virtual async Task Publish(string topic, object obj)
        {
            await this.Publish(this.AsMessage(topic, obj));
        }

        public virtual async Task Publish(string topic, object obj, Action callback)
        {
            await this.Publish(this.AsMessage(topic, obj), callback);
        }

        public virtual async Task Publish(string topic, byte[] data, object metadata)
        {
            await this.Publish(topic, data.ToList(), metadata);
        }
        public virtual async Task Publish(string topic, List<byte> data, object metadata)
        {
            await this.Publish(new Message(data, metadata, topic, this.ClientInfo.Controller));
        }
        public virtual async Task Publish(string topic, byte[] data)
        {
            await this.Publish(topic, data.ToList());
        }
        public virtual async Task Publish(string topic, List<byte> data)
        {
            await this.Publish(new Message(data, topic, this.ClientInfo.Controller));
        }        
    }    
}