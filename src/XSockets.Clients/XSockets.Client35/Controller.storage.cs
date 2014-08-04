using System;
using System.Threading;
using System.Threading.Tasks;
using XSockets.Client35.Common.Interfaces;
using XSockets.Client35.Globals;
using XSockets.Client35.Model;

namespace XSockets.Client35
{
    public partial class Controller : IController
    {
        private Task<XStorage> _StorageTarget(string s, XStorage o, CancellationTokenSource cts)
        {
            return new Task<XStorage>(() =>
            {
                var token = cts.Token;
                var data = default(XStorage);
                var topic = (s + ":" + o.Key).ToLower();

                var listener = new Listener(topic, message => { data = this.XSocketClient.Serializer.DeserializeFromString<XStorage>(message.Data); }, SubscriptionType.One);

                this.Listeners.AddOrUpdate(topic, listener);

                this.Publish(s, o);

                while (data == null)
                {
                    if (token.IsCancellationRequested)
                        return data;
                    Thread.Sleep(1);
                }
                return data;
            });
        }

        private Task<XStorage> StorageWaitFor(string topic, XStorage data, CancellationTokenSource cts)
        {
            var waiter = _StorageTarget(topic, data, cts);
            waiter.Start();
            return waiter;
        }

        public void StorageSet<T>(string key, T value) where T : class
        {
            this.Invoke(Constants.Events.Storage.Set, new XStorage { Key = key, Value = this.XSocketClient.Serializer.SerializeToString(value) });
        }

        public IListener StorageOnSet<T>(string key, Action<T> action) where T : class
        {
            var topic = (Constants.Events.Storage.Set + ":" + key).ToLower();
            IListener listener = new Listener(topic, action.Method, typeof(T)) { IsStorageObject = true, Controller = this};
            return this.Listeners.AddOrUpdate(topic, listener);
        }

        public T StorageGet<T>(string key)
        {
            var result = this.StorageWaitFor(Constants.Events.Storage.Get, new XStorage { Key = key }, new CancellationTokenSource()).Result;
            if (result.Value == null)
            {
                return default(T);
            }
            return this.XSocketClient.Serializer.DeserializeFromString<T>(result.Value.ToString());
        }
        
        public IListener StorageOnRemove<T>(string key, Action<T> action) where T : class
        {
            var topic = (Constants.Events.Storage.Remove + ":" + key).ToLower();
            IListener listener = new Listener(topic, action.Method, typeof(T)){ IsStorageObject = true,Controller = this};
            return this.Listeners.AddOrUpdate(topic, listener);
        }

        public void StorageRemove(string key)
        {
            this.Invoke(Constants.Events.Storage.Remove, new XStorage { Key = key });
        }

        public void StorageClear()
        {
            this.Invoke(Constants.Events.Storage.Clear);
        }
    }
}