using System;
using System.Threading;
using System.Threading.Tasks;
using XSockets.ClientPortableW8.Common.Interfaces;
using XSockets.ClientPortableW8.Globals;
using XSockets.ClientPortableW8.Model;

namespace XSockets.ClientPortableW8
{
    public partial class Controller : IController
    {
        private Task<XStorage> _StorageTarget(string s, XStorage o, CancellationTokenSource cts)
        {
            return new Task<XStorage>(() =>
            {
                //var token = cts.Token;
                //var data = default(XStorage);
                //var topic = (s + ":" + o.Key).ToLower();

                //var listener = new Listener(topic, message => { data = this.XSocketClient.Serializer.DeserializeFromString<XStorage>(message.Data); }, SubscriptionType.One);

                //this.Listeners.AddOrUpdate(topic, listener);

                //this.Invoke(s, o);

                //while (data == null)
                //{
                //    if (token.IsCancellationRequested)
                //        return data;
                //    Thread.Sleep(1);
                //}
                //return data;


                var data = default(XStorage);
                var topic = (s + ":" + o.Key).ToLower();
                var working = true;
                var listener = new Listener(topic,
                    message =>
                    {
                        data = this.XSocketClient.Serializer.DeserializeFromString<XStorage>(message.Data);
                        working = false;
                    },
                    SubscriptionType.One);

                this._listeners.AddOrUpdate(topic, listener);

                this.Invoke(s,o);
                var r = SpinWait.SpinUntil(() => working == false, 60000);

                if (r == false)
                {
                    this.DisposeListener(listener);
                    throw new TimeoutException("The server did not respond in the given time frame");
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

        public async Task StorageSet<T>(string key, T value)
        {
            await this.Invoke(Constants.Events.Storage.Set, new XStorage { Key = key, Value = this.XSocketClient.Serializer.SerializeToString(value) });
        }

        public IListener StorageOnSet<T>(string key, Action<T> action)
        {
            var topic = (Constants.Events.Storage.Set + ":" + key).ToLower();
            IListener listener = new Listener(topic, message =>
            {
                var xs = this.XSocketClient.Serializer.DeserializeFromString<XStorage>(message.Data);
                if (xs.Value == null)
                    action(default(T));
                else
                    action(this.XSocketClient.Serializer.DeserializeFromString<T>(xs.Value.ToString()));
            }) { Controller = this };
            return this._listeners.AddOrUpdate(topic, listener);
        }

        public T StorageGet<T>(string key)
        {
            //var v = this.Invoke<XStorage>(Constants.Events.Storage.Get, new XStorage {Key = key}).Result;
            //return this.XSocketClient.Serializer.DeserializeFromString<T>(v.Value.ToString());
            var result = this.StorageWaitFor(Constants.Events.Storage.Get, new XStorage { Key = key }, new CancellationTokenSource()).Result;
            if (result.Value == null)
            {
                return default(T);
            }
            return this.XSocketClient.Serializer.DeserializeFromString<T>(result.Value.ToString());
        }

        public IListener StorageOnRemove<T>(string key, Action<T> action) 
        {
            var topic = (Constants.Events.Storage.Remove + ":" + key).ToLower();
            IListener listener = new Listener(topic, message => {
                var xs = this.XSocketClient.Serializer.DeserializeFromString<XStorage>(message.Data);
                if (xs.Value == null)
                    action(default(T));
                else
                    action(this.XSocketClient.Serializer.DeserializeFromString<T>(xs.Value.ToString()));
            }) { Controller = this };
            return this._listeners.AddOrUpdate(topic, listener);
        }

        public async Task StorageRemove(string key)
        {
            await this.Invoke(Constants.Events.Storage.Remove, new XStorage { Key = key });
        }

        public async Task StorageClear()
        {
            await this.Invoke(Constants.Events.Storage.Clear);
        }
    }
}