using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using XSockets.Client40.Common.Interfaces;

namespace XSockets.Client40.Utility.Observables
{
    /// <summary>
    /// Generic observer that needs a GUID to monitor objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class XSocketsObserverPool<T>
    {
        private static readonly ConcurrentDictionary<Guid, IXSocketObservable<T>> Trackers;
        static XSocketsObserverPool()
        {
            Trackers = new ConcurrentDictionary<Guid, IXSocketObservable<T>>();
        }

        public static IXSocketObservable<T> GetTracker(Guid id)
        {
            return Trackers.ContainsKey(id) ? Trackers[id] : null;
        }

        public static ConcurrentDictionary<Guid, IXSocketObservable<T>> GetAllTrackers()
        {
            return Trackers;
        }

        private static void Track(Guid id)
        {
            if (!Trackers.ContainsKey(id))
                Trackers.TryAdd(id, new XSocketObservable(id));
        }

        public static void Subscribe(Guid id, Action onNotifyAction, Action onCompletedAction = null, Action onErrorAction = null)
        {
            Track(id);
            var tracker = GetTracker(id);
            if (tracker == null || tracker.Observers.Count(p => p.Id == id) > 0) return;
            var observer = new XSocketObserver(id, onNotifyAction, onCompletedAction, onErrorAction);
            observer.Subscribe(tracker);
        }
        public static void Subscribe(Guid id, Action<T> onNotifyAction, Action<T> onCompletedAction = null, Action<T> onErrorAction = null)
        {
            Track(id);
            var tracker = GetTracker(id);
            if (tracker == null || tracker.Observers.Count(p => p.Id == id) > 0) return;
            var observer = new XSocketObserver(id, onNotifyAction, onCompletedAction, onErrorAction);
            observer.Subscribe(tracker);
        }

        public static void RemoveTracker(Guid id)
        {
            if (!Trackers.ContainsKey(id)) return;

            IXSocketObservable<T> tracker;
            Trackers[id].Completed(id);
            Trackers.TryRemove(id, out tracker);
        }

        public static void RemoveAllTrackers()
        {
            var ids = Trackers.Select(xSocketEventTracker => xSocketEventTracker.Key).ToList();
            foreach (var guid in ids)
            {
                IXSocketObservable<T> tracker;
                Trackers[guid].Completed(guid);
                Trackers.TryRemove(guid, out tracker);
            }
        }

        public static void Notify(Guid id, T loc)
        {
            if (Trackers.ContainsKey(id))
                Trackers[id].Notify(id, loc);
        }

        public static void Completed(Guid id)
        {
            if (Trackers.ContainsKey(id))
                Trackers[id].Completed(id);
        }


        /// <summary>
        /// Observer class
        /// Will observe an object in the XSocketsObserverPool
        /// Actions can be called onNotiy, onComplete and onError
        /// </summary>
        private sealed class XSocketObserver : IXSocketObserver<T>
        {
            public Guid Id { get; private set; }
            private IDisposable _unsubscriber;
            private readonly Action _onNotifyAction;
            private readonly Action<T> _onNotifyActionWithArgument;
            private readonly Action _onCompletedAction;
            private readonly Action<T> _onCompletedActionWithArgument;
            private readonly Action _onErrorAction;
            private readonly Action<T> _onErrorActionWithArgument;

            public XSocketObserver(Guid id, Action onNotifyAction, Action onCompletedAction, Action onErrorAction)
            {
                this.Id = id;
                this._onNotifyAction = onNotifyAction;
                this._onCompletedAction = onCompletedAction;
                this._onErrorAction = onErrorAction;
            }

            public XSocketObserver(Guid id, Action<T> onNotifyAction, Action<T> onCompletedAction, Action<T> onErrorAction)
            {
                this.Id = id;
                this._onNotifyActionWithArgument = onNotifyAction;
                this._onCompletedActionWithArgument = onCompletedAction;
                this._onErrorActionWithArgument = onErrorAction;
            }

            public void Subscribe(IXSocketObservable<T> provider)
            {
                if (provider != null)
                    _unsubscriber = provider.Subscribe(this);
            }

            public void OnCompleted()
            {
                if (this._onCompletedAction != null) this._onCompletedAction.Invoke();
                this.Unsubscribe();
            }

            public void OnError(Exception e)
            {
                if (this._onErrorAction != null) this._onErrorAction.Invoke();
            }

            public void OnNotify(T value)
            {
                if (this._onNotifyAction != null)
                {                
                    this._onNotifyAction.Invoke();
                    return;                    
                }
                if (this._onNotifyActionWithArgument != null) this._onNotifyActionWithArgument.Invoke(value);
            }

            public void Unsubscribe()
            {
                if (_unsubscriber != null)
                    _unsubscriber.Dispose();
            }
        }

        /// <summary>
        /// Observable class
        /// Will be called from the Observers Subscribe method
        /// Will notify the Observer when 
        /// </summary>
        private sealed class XSocketObservable : IXSocketObservable<T>
        {
            public Guid Id { get; private set; }
            public ISet<IXSocketObserver<T>> Observers { get; private set; }

            public XSocketObservable(Guid id)
            {
                this.Id = id;
                this.Observers = new HashSet<IXSocketObserver<T>>();
            }

            public IDisposable Subscribe(IXSocketObserver<T> observer)
            {
                if (!Observers.Contains(observer))
                    Observers.Add(observer);
                return new Unsubscriber(Observers, observer);
            }

            private class Unsubscriber : IDisposable
            {
                private readonly ISet<IXSocketObserver<T>> _observers;
                private readonly IXSocketObserver<T> _observer;

                public Unsubscriber(ISet<IXSocketObserver<T>> observers, IXSocketObserver<T> observer)
                {
                    this._observers = observers;
                    this._observer = observer;
                }

                public void Dispose()
                {
                    if (_observer != null && _observers.Contains(_observer))
                        _observers.Remove(_observer);
                }
            }

            void IXSocketObservable<T>.Notify(Guid id, T loc)
            {
                var observer = this.Observers.SingleOrDefault(p => p.Id == id);
                if (observer == null) return;

                observer.OnNotify(loc);
            }

            void IXSocketObservable<T>.Completed(Guid id)
            {
                var observer = this.Observers.SingleOrDefault(p => p.Id == id);
                if (observer == null) return;

                observer.OnCompleted();
            }
        }
    }
}