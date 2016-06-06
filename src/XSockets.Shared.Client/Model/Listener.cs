namespace XSockets.Model
{
    using System;
    using XSockets.Common.Interfaces;

    public class Listener : IListener
    {
        public Listener(string topic, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0, bool confirm = false)
        {
            this.Topic = topic.ToLower();
            this.SubscriptionType = subscriptionType;
            this.Counter = 0;
            this.Limit = limit;
            this.IsPrimitive = true;
            this.Confirm = confirm;
        }
        public Listener(string topic, Action<IMessage> action, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0, bool confirm = false)
        {
            this.Topic = topic.ToLower();
            this.Callback = action;
            this.SubscriptionType = subscriptionType;
            this.Counter = 0;
            this.Limit = limit;
            this.Confirm = confirm;
        }

        //public Listener(string topic, MethodInfo action, Type parameter, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0, bool confirm = false)
        //{
        //    this.Topic = topic.ToLower();
        //    this.mi = action;
        //    this.Type = parameter;
        //    this.SubscriptionType = subscriptionType;
        //    this.Counter = 0;
        //    this.Limit = limit;
        //    this.Confirm = confirm;
        //}

        public string Topic { get; set; }
        public bool IsPrimitive { get; set; }
        public bool IsBound { get; set; }
        public bool Confirm { get; set; }
        public uint Counter { get; set; }
        public uint Limit { get; set; }
        //public bool IsStorageObject { get; set; }

        public SubscriptionType SubscriptionType { get; set; }
        public Action<IMessage> Callback { get; set; }
        public Type Type { get; private set; }
        //private MethodInfo mi;
        public IController Controller { get; set; }
        //public void Execute(params object[] param)
        //{
        //    var actionT = typeof(Action<>).MakeGenericType(this.Type);
        //    Delegate.CreateDelegate(actionT, this.mi).DynamicInvoke(param);
        //}

        public void Dispose()
        {
            if (this.Controller != null)
            {
                this.Controller.DisposeListener(this);
            }
        }
    }
}