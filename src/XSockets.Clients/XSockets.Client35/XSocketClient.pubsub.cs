//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using XSockets.Client35.Common.Event.Interface;
//using XSockets.Client35.Common.Interfaces;
//using XSockets.Client35.Globals;
//using XSockets.Client35.Model;
//using XSockets.Client35.Utility.Storage;

//namespace XSockets.Client35
//{
//    /// <summary>
//    /// A client for communicating with XSockets over pub/sub
//    /// </summary>
//    public partial class XSocketClient
//    {
//        private RepositoryInstance<string, Subscription> Subscriptions;

//        private void AddDefaultSubscriptions()
//        {
//            var onError = new Subscription(Constants.Events.OnError, Error) { IsBound = true };
//            this.Subscriptions.AddOrUpdate(Constants.Events.OnError, onError);
//            var onOpen = new Subscription(Constants.Events.Connections.Opened, Opened) { IsBound = true };
//            this.Subscriptions.AddOrUpdate(Constants.Events.Connections.Opened, onOpen);
//        }

//        private void BindUnboundSubscriptions()
//        {
//            var unboundBindings = this.Subscriptions.Find(p => p.IsBound == false).ToList();

//            if (!unboundBindings.Any()) return;
//            foreach (var unboundBinding in unboundBindings)
//            {
//                var binding = unboundBinding;
//                new Task(
//                    () =>
//                    Publish(this.AsTextArgs(new XSubscriptions { Event = binding.Event.ToLower(), Confirm = binding.Confirm }
//                                         , Constants.Events.PubSub.Subscribe), () =>
//                                         {
//                                             var b = this.Subscriptions.GetById(binding.Event);
//                                             b.IsBound = true;
//                                             this.Subscriptions.AddOrUpdate(binding.Event, b);
//                                         })).RunSynchronously();
//            }
//        }

//        public void One(string name, Action<ITextArgs> callback)
//        {
//            this.Subscribe(name, callback, SubscriptionType.One);
//        }

//        public void One<T>(string name, Action<T> callback) where T : class
//        {
//            this.Subscribe(name, callback, SubscriptionType.One);
//        }

//        public void One(string name, Action<ITextArgs> callback, Action<ITextArgs> confirmCallback)
//        {
//            this.Subscribe(name, callback, confirmCallback, SubscriptionType.One);
//        }

//        public void One<T>(string name, Action<T> callback, Action<ITextArgs> confirmCallback) where T : class
//        {
//            this.Subscribe(name, callback, confirmCallback, SubscriptionType.One);
//        }

//        public void Many(string name, uint limit, Action<ITextArgs> callback)
//        {
//            this.Subscribe(name, callback, SubscriptionType.Many, limit);
//        }

//        public void Many<T>(string name, uint limit, Action<T> callback) where T : class
//        {
//            this.Subscribe(name, callback, SubscriptionType.Many, limit);
//        }

//        public void Many(string name, uint limit, Action<ITextArgs> callback, Action<ITextArgs> confirmCallback)
//        {
//            this.Subscribe(name, callback, confirmCallback, SubscriptionType.Many, limit);
//        }

//        public void Many<T>(string name, uint limit, Action<T> callback, Action<ITextArgs> confirmCallback) where T : class
//        {
//            this.Subscribe(name, callback, confirmCallback, SubscriptionType.Many, limit);
//        }

//        private void AddConfirmCallback(Action<ITextArgs> confirmCallback, string @event)
//        {
//            var e = string.Format("__{0}", @event);
//            if (this.Subscriptions.ContainsKey(e)) return;

//            var confirm = new Subscription(e, confirmCallback);
//            confirm.IsBound = this.IsConnected;
//            this.Subscriptions.AddOrUpdate(e, confirm);
//        }

//        /// <summary>
//        /// Remove the subscription from the list
//        /// </summary>
//        /// <param name="name"></param>
//        public void Unsubscribe(string name)
//        {
//            ISubscription subscription = Repository<string, Subscription>.GetById(name.ToLower());
//            if (subscription == null) return;

//            if (this.IsConnected)
//            {
//                //Unbind on server
//                Publish(this.AsTextArgs(new XSubscriptions { Event = name.ToLower() }, Constants.Events.PubSub.Unsubscribe));
//            }
//            Repository<string, Subscription>.Remove(name.ToLower());
//        }


//        public void Subscribe(string name)
//        {
//            this.Subscribe(name, SubscriptionType.All);
//        }
//        public void Subscribe(string name, SubscriptionType subscriptionType, uint limit = 0)
//        {
//            var subscription = new Subscription(name, subscriptionType, limit);
//            this.Subscriptions.AddOrUpdate(name.ToLower(), subscription);

//            if (this.IsConnected)
//            {
//                Publish(this.AsTextArgs(new XSubscriptions
//                {
//                    Event = name.ToLower()
//                }, Constants.Events.PubSub.Subscribe), () => { subscription.IsBound = true; });
//            }
//        }
//        public void Subscribe(string name, Action<ITextArgs> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0)
//        {
//            var subscription = new Subscription(name, callback, subscriptionType, limit);
//            this.Subscriptions.AddOrUpdate(name.ToLower(), subscription);

//            if (this.IsConnected)
//            {
//                Publish(this.AsTextArgs(new XSubscriptions
//                {
//                    Event = name.ToLower()
//                }, Constants.Events.PubSub.Subscribe), () => { subscription.IsBound = true; });
//            }
//        }
//        public void Subscribe(string name, Action<ITextArgs> callback, Action<ITextArgs> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0)
//        {
//            var subscription = new Subscription(name.ToLower(), callback, subscriptionType, limit, true);
//            this.Subscriptions.AddOrUpdate(name.ToLower(), subscription);

//            AddConfirmCallback(confirmCallback, subscription.Event);
//            if (this.IsConnected)
//            {
//                Publish(this.AsTextArgs(new XSubscriptions { Event = name.ToLower(), Confirm = true }, Constants.Events.PubSub.Subscribe), () => subscription.IsBound = true);
//            }
//        }

//        public void Subscribe<T>(string topic, Action<T> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0) where T : class
//        {
//            var subscription = new Subscription(topic.ToLower(), callback.Method, typeof(T), subscriptionType, limit);
//            this.Subscriptions.AddOrUpdate(topic, subscription);
//            if (this.IsConnected)
//            {
//                Publish(this.AsTextArgs(new XSubscriptions { Event = topic.ToLower(), Confirm = false }, Constants.Events.PubSub.Subscribe), () => subscription.IsBound = true);
//            }
//        }

//        public void Subscribe<T>(string topic, Action<T> callback, Action<ITextArgs> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0) where T : class
//        {
//            var subscription = new Subscription(topic.ToLower(), callback.Method, typeof(T), subscriptionType, limit, true);
//            this.Subscriptions.AddOrUpdate(topic, subscription);
//            AddConfirmCallback(confirmCallback, subscription.Event);
//            if (this.IsConnected)
//            {
//                Publish(this.AsTextArgs(new XSubscriptions { Event = topic.ToLower(), Confirm = true }, Constants.Events.PubSub.Subscribe), () => subscription.IsBound = true);
//            }
//        }
//        //Sending methods

//        /// <summary>
//        ///     Send message
//        /// </summary>
//        /// <param name="payload">ITextArgs</param>
//        /// <param name="callback"> </param>
//        public void Publish(ITextArgs payload, Action callback)
//        {
//            if (!this.IsConnected)
//                throw new Exception("You cant send messages when not conencted to the server");
//            var frame = GetDataFrame(payload);
//            Socket.Send(frame.ToBytes(), callback.Invoke, err => FireOnClose());
//        }

//        /// <summary>
//        ///     Send a binary message)
//        /// </summary>
//        /// <param name="payload">IBinaryArgs</param>
//        public void Publish(IBinaryArgs payload)
//        {
//            if (!this.IsConnected)
//                throw new Exception("You cant send messages when not conencted to the server");
//            var frame = GetDataFrame(payload);
//            Socket.Send(frame.ToBytes(), () => { }, ex => { });
//        }

//        public void Publish(string payload)
//        {
//            this.Publish(payload, () => { });
//            //if (!this.IsConnected)
//            //    throw new Exception("You cant send messages when not conencted to the server");
//            //var frame = GetDataFrame(payload);
//            //Socket.Send(frame.ToBytes(), () => { }, err => FireOnClose());
//        }

//        public void Publish(string payload, Action callback)
//        {
//            if (!this.IsConnected)
//                throw new Exception("You cant send messages when not conencted to the server");
//            var frame = GetDataFrame(payload);
//            Socket.Send(frame.ToBytes(), callback.Invoke, err => FireOnClose());
//        }

//        public void Publish(ITextArgs payload)
//        {
//            if (!this.IsConnected)
//                throw new Exception("You cant send messages when not conencted to the server");
//            var frame = GetDataFrame(payload);
//            Socket.Send(frame.ToBytes(), () => { }, err => FireOnClose());
//        }

//        public void Publish(object obj, string @event)
//        {
//            this.Publish(this.AsTextArgs(obj, @event));
//        }

//        public void Publish(object obj, string @event, Action callback)
//        {
//            this.Publish(this.AsTextArgs(obj, @event), callback);
//        }
//    }
//    ///// <summary>
//    ///// A client for communicating with XSockets over pub/sub
//    ///// </summary>
//    //public partial class XSocketClient
//    //{
//    //    private IList<ISubscription> Subscriptions { get; set; }

//    //    private void AddDefaultSubscriptions()
//    //    {
//    //        var onError = new Subscription(Constants.Events.OnError, Error) { IsBound = true };
//    //        this.AddSubscription(onError);
//    //        var onOpen = new Subscription(Constants.Events.Connections.Opened, Opened) { IsBound = true };
//    //        this.AddSubscription(onOpen);
//    //    }

//    //    public IList<ISubscription> GetSubscriptions()
//    //    {
//    //        lock (this.Subscriptions)
//    //        {            
//    //            return this.Subscriptions.ToList();
//    //        }
//    //    }
//    //    private void AddSubscription(ISubscription subscription)
//    //    {
//    //        lock (this.Subscriptions)
//    //        {
//    //            this.Subscriptions.Add(subscription);
//    //        }
//    //    }

//    //    private void RemoveSubscription(ISubscription subscription)
//    //    {
//    //        lock (this.Subscriptions)
//    //        {
//    //            this.Subscriptions.Remove(subscription);
//    //        }
//    //    }

//    //    private void BindUnboundSubscriptions()
//    //    {
//    //        var unboundBindings = this.GetSubscriptions().Where(p => p.IsBound == false).ToList();

//    //        if (!unboundBindings.Any()) return;
//    //        foreach (var unboundBinding in unboundBindings)
//    //        {
//    //            var binding = unboundBinding;
//    //            new Task(
//    //                () =>
//    //                Publish(this.AsTextArgs(new XSubscriptions {Event = binding.Event.ToLower(), Confirm = binding.Confirm}
//    //                                     , Constants.Events.PubSub.Subscribe), () =>
//    //                                         {
//    //                                             var b = this.GetSubscriptions().Single(p => p.Event == binding.Event);
//    //                                             b.IsBound = true;
//    //                                         })).RunSynchronously();
//    //        }
//    //    }

//    //    public void One(string name, Action<ITextArgs> callback)
//    //    {
//    //        this.Subscribe(name, callback, SubscriptionType.One);    
//    //    }

//    //    public void One(string name, Action<ITextArgs> callback, Action<ITextArgs> confirmCallback)
//    //    {
//    //        this.Subscribe(name, callback, confirmCallback, SubscriptionType.One);
//    //    }

//    //    public void Many(string name, uint limit, Action<ITextArgs> callback)
//    //    {
//    //        this.Subscribe(name, callback, SubscriptionType.Many,limit);
//    //    }

//    //    public void Many(string name, uint limit, Action<ITextArgs> callback, Action<ITextArgs> confirmCallback)
//    //    {
//    //        this.Subscribe(name,callback,confirmCallback, SubscriptionType.Many, limit);
//    //    }

//    //    private void AddConfirmCallback(Action<ITextArgs> confirmCallback, string @event)
//    //    {
//    //        var e = string.Format("__{0}", @event);
//    //        if (this.GetSubscriptions().Any(p => p.Event == e)) return;

//    //        var confirm = new Subscription(e, confirmCallback);
//    //        this.AddSubscription(confirm);
//    //        confirm.IsBound = this.IsConnected;
//    //    }

//    //    /// <summary>
//    //    /// Remove the subscription from the list
//    //    /// </summary>
//    //    /// <param name="name"></param>
//    //    public void Unsubscribe(string name)
//    //    {
//    //        ISubscription subscription = this.GetSubscriptions().FirstOrDefault(b => b.Event.Equals(name.ToLower()));
//    //        if (subscription == null) return;

//    //        if (this.IsConnected)
//    //        {
//    //            //Unbind on server
//    //            Publish(this.AsTextArgs(new XSubscriptions {Event = name.ToLower()}, Constants.Events.PubSub.Unsubscribe));
//    //        }

//    //        this.RemoveSubscription(subscription);
//    //    }


//    //    public void Subscribe(string name)
//    //    {
//    //        this.Subscribe(name, SubscriptionType.All);
//    //    }
//    //    public void Subscribe(string name, SubscriptionType subscriptionType, uint limit = 0)
//    //    {
//    //        var subscription = new Subscription(name, subscriptionType, limit);
//    //        this.AddSubscription(subscription);

//    //        if (this.IsConnected)
//    //        {
//    //            Publish(this.AsTextArgs(new XSubscriptions
//    //            {
//    //                Event = name.ToLower()                    
//    //            }, Constants.Events.PubSub.Subscribe), () => { subscription.IsBound = true; });
//    //        }
//    //    }
//    //    public void Subscribe(string name, Action<ITextArgs> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0)
//    //    {
//    //        var subscription = new Subscription(name, callback, subscriptionType, limit);
//    //        this.AddSubscription(subscription);

//    //        if (this.IsConnected)
//    //        {
//    //            Publish(this.AsTextArgs(new XSubscriptions
//    //            {
//    //                Event = name.ToLower()                    
//    //            }, Constants.Events.PubSub.Subscribe), () => { subscription.IsBound = true; });
//    //        }
//    //    }
//    //    public void Subscribe(string name, Action<ITextArgs> callback, Action<ITextArgs> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0)
//    //    {
//    //        var subscription = new Subscription(name.ToLower(), callback, subscriptionType, limit, true);
//    //        this.AddSubscription(subscription);
//    //        AddConfirmCallback(confirmCallback, subscription.Event);
//    //        if (this.IsConnected)
//    //        {                
//    //            Publish(this.AsTextArgs(new XSubscriptions { Event = name.ToLower(), Confirm = true}, Constants.Events.PubSub.Subscribe), () => subscription.IsBound = true);
//    //        }
//    //    }        
//    //    //Sending methods

//    //    /// <summary>
//    //    ///     Send message
//    //    /// </summary>
//    //    /// <param name="payload">ITextArgs</param>
//    //    /// <param name="callback"> </param>
//    //    public void Publish(ITextArgs payload, Action callback)
//    //    {
//    //        if (!this.IsConnected)
//    //            throw new Exception("You cant send messages when not conencted to the server");
//    //        var frame = GetDataFrame(payload);
//    //        Socket.Send(frame.ToBytes(), callback.Invoke, err => FireOnClose());
//    //    }

//    //    /// <summary>
//    //    ///     Send a binary message)
//    //    /// </summary>
//    //    /// <param name="payload">IBinaryArgs</param>
//    //    public void Publish(IBinaryArgs payload)
//    //    {
//    //        if (!this.IsConnected)
//    //            throw new Exception("You cant send messages when not conencted to the server");
//    //        var frame = GetDataFrame(payload);
//    //        Socket.Send(frame.ToBytes(), () => { }, ex => { });
//    //    }

//    //    public void Publish(string payload)
//    //    {
//    //        if (!this.IsConnected)
//    //            throw new Exception("You cant send messages when not conencted to the server");
//    //        var frame = GetDataFrame(payload);
//    //        Socket.Send(frame.ToBytes(), () => { }, err => FireOnClose());
//    //    }

//    //    public void Publish(string payload, Action callback)
//    //    {
//    //        if (!this.IsConnected)
//    //            throw new Exception("You cant send messages when not conencted to the server");
//    //        var frame = GetDataFrame(payload);
//    //        Socket.Send(frame.ToBytes(), callback.Invoke, err => FireOnClose());
//    //    }

//    //    public void Publish(ITextArgs payload)
//    //    {
//    //        if (!this.IsConnected)
//    //            throw new Exception("You cant send messages when not conencted to the server");
//    //        var frame = GetDataFrame(payload);
//    //        Socket.Send(frame.ToBytes(), () => { }, err => FireOnClose());
//    //    }

//    //    public void Publish(object obj, string @event)
//    //    {
//    //        this.Publish(this.AsTextArgs(obj, @event));
//    //    }

//    //    public void Publish(object obj, string @event, Action callback)
//    //    {
//    //        this.Publish(this.AsTextArgs(obj, @event), callback);
//    //    }
//    //}
//}