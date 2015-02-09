using System;

namespace XSockets.ClientIOS.Common.Interfaces
{
    public interface IXSocketObserver<T>
    {
        Guid Id { get; }
        void OnError(Exception e);
        void OnNotify(T value);
        void Unsubscribe();
        void Subscribe(IXSocketObservable<T> provider);
        void OnCompleted();
    }
}