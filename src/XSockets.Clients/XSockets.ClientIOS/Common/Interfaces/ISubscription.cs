using System;
using XSockets.ClientIOS.Model;

namespace XSockets.ClientIOS.Common.Interfaces
{
    public interface IDispatcher
    {
        string Topic { get; set; }
        uint Counter { get; set; }
        uint Limit { get; set; }
        SubscriptionType SubscriptionType { get; set; }
        Action<IMessage> Callback { get; set; }
        //void Execute(params object[] param);
        Type Type { get; }
        
    }

    public interface IListener : IDispatcher, IDisposable
    {
        //bool IsStorageObject { get; set; }
        IController Controller { get; set; }
    }
    public interface ISubscription: IDispatcher
    {        
        bool IsBound { get; set; }
        bool IsPrimitive { get; set; }
        bool Confirm { get; set; }    
    }
}