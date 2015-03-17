using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XSockets.ClientIOS.Common.Interfaces;
using XSockets.ClientIOS.Globals;
using XSockets.ClientIOS.Model;

namespace XSockets.ClientIOS
{    
    public partial class Controller : IController
    {        
        private void AddDefaultSubscriptions()
        {
            var onError = new Subscription(Constants.Events.Error, Error) { IsBound = true };
            this.Subscriptions.AddOrUpdate(Constants.Events.Error, onError);
            var onOpen = new Subscription(Constants.Events.Controller.Opened, Opened) { IsBound = true };
            this.Subscriptions.AddOrUpdate(Constants.Events.Controller.Opened, onOpen);
            var onClose = new Subscription(Constants.Events.Controller.Closed, Closed) { IsBound = true };
            this.Subscriptions.AddOrUpdate(Constants.Events.Controller.Closed, onClose);
        }

        private void ResetSubscriptions()
        {
            foreach (var s in this.Subscriptions.GetAllWithKeys())
            {
                if(s.Key == Constants.Events.Controller.Closed || s.Key == Constants.Events.Controller.Opened || s.Key == Constants.Events.Error) continue;

                s.Value.IsBound = false;
                this.Subscriptions.AddOrUpdate(s.Key, s.Value);
            }
        }

        public virtual void BindUnboundSubscriptions()
        {
            var unboundBindings = this.Subscriptions.Find(p => p.IsBound == false).ToList();

            if (!unboundBindings.Any()) return;
            foreach (var unboundBinding in unboundBindings)
            {
                var binding = unboundBinding;
                new Task(
                    () =>
                    Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = binding.Topic.ToLower(), Ack = binding.Confirm }), () =>
                    {
                        var b = this.Subscriptions.GetById(binding.Topic);
                        b.IsBound = true;
                        this.Subscriptions.AddOrUpdate(binding.Topic, b);
                    })).RunSynchronously();
            }
        }

        public virtual IMessage AsMessage(string topic, object o)
        {
            return new Message(o, topic, this.ClientInfo.Controller);
        }

        public virtual void One(string topic, Action<IMessage> callback)
        {
            this.Subscribe(topic, callback, SubscriptionType.One);
        }

        public virtual void One<T>(string topic, Action<T> callback)
        {
            this.Subscribe(topic, callback, SubscriptionType.One);
        }

        public virtual void One(string topic, Action<IMessage> callback, Action<IMessage> confirmCallback)
        {
            this.Subscribe(topic, callback, confirmCallback, SubscriptionType.One);
        }

        public virtual void One<T>(string topic, Action<T> callback, Action<IMessage> confirmCallback)
        {
            this.Subscribe(topic, callback, confirmCallback, SubscriptionType.One);
        }

        public virtual void Many(string topic, uint limit, Action<IMessage> callback)
        {
            this.Subscribe(topic, callback, SubscriptionType.Many, limit);
        }

        public virtual void Many<T>(string topic, uint limit, Action<T> callback)
        {
            this.Subscribe(topic, callback, SubscriptionType.Many, limit);
        }

        public virtual void Many(string topic, uint limit, Action<IMessage> callback, Action<IMessage> confirmCallback)
        {
            this.Subscribe(topic, callback, confirmCallback, SubscriptionType.Many, limit);
        }

        public virtual void Many<T>(string topic, uint limit, Action<T> callback, Action<IMessage> confirmCallback)
        {
            this.Subscribe(topic, callback, confirmCallback, SubscriptionType.Many, limit);
        }

        private void AddConfirmCallback(Action<IMessage> confirmCallback, string @event)
        {
            var e = string.Format("__{0}", @event);

            if (@event.Contains("."))
            {
                var info = @event.Split('.');
                e = string.Format("{0}.__{1}", info[0], info[1]);
            }
            if (this.Subscriptions.ContainsKey(e)) return;

            var confirm = new Subscription(e, confirmCallback);
            confirm.IsBound = this.XSocketClient.IsConnected;
            this.Subscriptions.AddOrUpdate(e, confirm);
        }

        /// <summary>
        /// Remove the subscription from the list
        /// </summary>
        /// <param name="topic"></param>
        public virtual void Unsubscribe(string topic)
        {
            topic = topic.ToLower();
            ISubscription subscription = this.Subscriptions.GetById(topic);

            if (subscription == null)
            {
                subscription = this.Subscriptions.GetById(topic);
            }

            if (subscription == null) return;

            if (this.XSocketClient.IsConnected)
            {
                Publish(this.AsMessage(Constants.Events.PubSub.Unsubscribe, new XSubscription { Topic = topic }));
            }
            this.Subscriptions.Remove(topic);
        }

        public virtual void Subscribe(string topic)
        {
            this.Subscribe(topic, SubscriptionType.All);
        }

        public virtual void Subscribe(string topic, SubscriptionType subscriptionType, uint limit = 0)
        {
            var subscription = new Subscription(topic, subscriptionType, limit);
            this.Subscriptions.AddOrUpdate(subscription.Topic, subscription);

            if (this.XSocketClient.IsConnected)
            {
                Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription
                {
                    Topic = subscription.Topic
                }), () => { subscription.IsBound = true; });
            }
        }
        public virtual void Subscribe(string topic, Action<IMessage> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0)
        {
            var subscription = new Subscription(topic, callback, subscriptionType, limit);
            this.Subscriptions.AddOrUpdate(subscription.Topic, subscription);

            if (this.XSocketClient.IsConnected)
            {
                Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription
                {
                    Topic = subscription.Topic
                }), () => { subscription.IsBound = true; });
            }
        }
        public virtual void Subscribe(string topic, Action<IMessage> callback, Action<IMessage> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0)
        {
            var subscription = new Subscription(topic, callback, subscriptionType, limit, true);
            this.Subscriptions.AddOrUpdate(subscription.Topic, subscription);

            AddConfirmCallback(confirmCallback, subscription.Topic);
            if (this.XSocketClient.IsConnected)
            {
                Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = subscription.Topic, Ack = true, Controller = this.ClientInfo.Controller }), () => subscription.IsBound = true);
            }
        }

        public virtual void Subscribe<T>(string topic, Action<T> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0) //where T : class
        {
            var subscription = new Subscription(topic, message => callback(this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data)), subscriptionType, limit);

            this.Subscriptions.AddOrUpdate(subscription.Topic, subscription);
            if (this.XSocketClient.IsConnected)
            {
                Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = subscription.Topic, Ack = false, Controller = this.ClientInfo.Controller }), () => subscription.IsBound = true);
            }
        }

        public virtual void Subscribe<T>(string topic, Action<T> callback, Action<IMessage> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0) //where T : class
        {            
            var subscription = new Subscription(topic, message => callback(this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data)), subscriptionType, limit);

            this.Subscriptions.AddOrUpdate(subscription.Topic, subscription);
            AddConfirmCallback(confirmCallback, subscription.Topic);
            if (this.XSocketClient.IsConnected)
            {
                Publish(this.AsMessage(Constants.Events.PubSub.Subscribe, new XSubscription { Topic = subscription.Topic, Ack = true, Controller = this.ClientInfo.Controller }), () => subscription.IsBound = true);
            }
        }
        //Sending methods

        /// <summary>
        ///     Send message
        /// </summary>
        /// <param name="payload">IMessage</param>
        /// <param name="callback"> </param>
        public virtual void Publish(IMessage payload, Action callback)
        {
            if (!this.XSocketClient.IsConnected)
                throw new Exception("You cant send messages when not connected to the server");
            
            payload.Controller = this.ClientInfo.Controller;
            var frame = GetDataFrame(payload).ToBytes();
            //If controller not yet open... Queue message
            if (this.ClientInfo.ConnectionId == Guid.Empty)
            {
                this.queuedFrames.AddRange(frame);
                return;
            }

            this.XSocketClient.Socket.Send(frame, callback.Invoke, err => FireClosed());
        }

        public virtual void Publish(string payload, Action callback)
        {
            if (!this.XSocketClient.IsConnected)
                throw new Exception("You cant send messages when not connected to the server");

            var frame = GetDataFrame(payload).ToBytes();
            //If controller not yet open... Queue message
            if (this.ClientInfo.ConnectionId == Guid.Empty)
            {
                this.queuedFrames.AddRange(frame);
                return;
            }
            this.XSocketClient.Socket.Send(frame, callback.Invoke, err => FireClosed());
        }

        public virtual void Publish(IMessage payload)
        {
            if (!this.XSocketClient.IsConnected)
                throw new Exception("You cant send messages when not connected to the server");
            
            payload.Controller = this.ClientInfo.Controller;
            var frame = GetDataFrame(payload).ToBytes();
            //If controller not yet open... Queue message
            if (this.ClientInfo.ConnectionId == Guid.Empty)
            {
                this.queuedFrames.AddRange(frame);
                return;
            }

            this.XSocketClient.Socket.Send(frame, () => { }, err => FireClosed());
        }

        public virtual void Publish(string topic)
        {
            this.Publish(this.AsMessage(topic,null));
        }

        public virtual void Publish(string topic, object obj)
        {
            this.Publish(this.AsMessage(topic, obj));
        }

        public virtual void Publish(string topic, object obj, Action callback)
        {
            this.Publish(this.AsMessage(topic, obj), callback);
        }

        public virtual void Publish(string topic, byte[] data, object metadata)
        {
            this.Publish(topic, data.ToList(), metadata);
        }
        public virtual void Publish(string topic, List<byte> data, object metadata)
        {
            this.Publish(new Message(data, metadata, topic, this.ClientInfo.Controller));
        }
        public virtual void Publish(string topic, byte[] data)
        {
            this.Publish(topic, data.ToList());
        }
        public virtual void Publish(string topic, List<byte> data)
        {
            this.Publish(new Message(data, topic, this.ClientInfo.Controller));
        }        
    }    
}