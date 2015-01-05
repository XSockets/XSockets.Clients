using System;
using XSockets.ClientPortableW8.Common.Interfaces;

namespace XSockets.ClientPortableW8.Model
{
    public class Subscription : ISubscription
    {
        public Subscription(string topic, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0, bool confirm = false)
        {
            this.Topic = topic.ToLower();
            this.SubscriptionType = subscriptionType;
            this.Counter = 0;
            this.Limit = limit;
            this.IsPrimitive = true;
            this.Confirm = confirm;
        }
        public Subscription(string topic, Action<IMessage> action, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0, bool confirm = false)
        {
            this.Topic = topic.ToLower();
            this.Callback = action;
            this.SubscriptionType = subscriptionType;
            this.Counter = 0;
            this.Limit = limit;
            this.Confirm = confirm;
        }

        public string Topic { get; set; }
        public bool IsPrimitive { get; set; }
        public bool IsBound { get; set; }
        public bool Confirm { get; set; }
        public uint Counter { get; set; }
        public uint Limit { get; set; }        
        
        public SubscriptionType SubscriptionType { get; set; }
        public Action<IMessage> Callback { get; set; }
        public Type Type { get; private set; }        
    }
}