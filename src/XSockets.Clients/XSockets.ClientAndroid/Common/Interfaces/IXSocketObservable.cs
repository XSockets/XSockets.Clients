using System;
using System.Collections.Generic;

namespace XSockets.ClientAndroid.Common.Interfaces
{
    public interface IXSocketObservable<T>
    {
        Guid Id { get; }
        IDisposable Subscribe(IXSocketObserver<T> observer);
        ISet<IXSocketObserver<T>> Observers { get; }
        void Notify(Guid id, T loc);
        void Completed(Guid id);
    }
}