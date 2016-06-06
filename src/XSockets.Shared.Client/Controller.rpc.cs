namespace XSockets
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using XSockets.Common.Interfaces;
    using XSockets.Model;

    public partial class Controller : IController
    {
        private async Task<T> _Target<T>(IMessage m, int timeoutMilliseconds = 30000)
        {
            //var tt = Task<T>.Run(async () => { });
            return await Task<T>.Run(async () =>
            {
                var data = default(T);
                var working = true;
                var listener = new Listener(m.Topic,
                    message =>
                    {                        
                        data = this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data);
                        working = false;
                    },
                    SubscriptionType.One);

                this._listeners.AddOrUpdate(m.Topic, listener);

                await this.Invoke(m);
                var r = SpinWait.SpinUntil(() => working == false, timeoutMilliseconds);

                if (r == false)
                {
                    this.DisposeListener(listener);
                    throw new TimeoutException("The server did not respond in the given time frame");
                }


                return data;
            });
            //t.Start();
            //return t;
        }

        private async Task<T> _Target<T>(string s, int timeoutMilliseconds = 30000)
        {
            s = s.ToLower();
            return await Task<T>.Run(async () =>
            {
                var data = default(T);
                var working = true;
                var listener = new Listener(s,
                    message =>
                    {
                        data = this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data); working = false;
                    },
                    SubscriptionType.One);

                this._listeners.AddOrUpdate(s, listener);

                //TODO: AtLeastOnce would be nice?
                await this.Invoke(this.AsMessage(s, null, QoS.FireAndForget, false));

                var r = SpinWait.SpinUntil(() => working == false, timeoutMilliseconds);

                if (r == false)
                {
                    this.DisposeListener(listener);
                    throw new TimeoutException("The server did not respond in the given time frame");
                }

                return data;
            });
            //t.Start();
            //return t;
        }        

        public virtual async Task Invoke(IMessage payload, bool isAck = false)
        {
            //TODO: Check and handle QoS
            if (!this.XSocketClient.IsConnected)
                throw new Exception("You cant send messages when not connected to the server");

            payload.Controller = this.ClientInfo.Controller;
            await this.SendAsync(payload, isAck);      
        }

        public virtual async Task Invoke(string target)
        {
            await this.Invoke(this.AsMessage(target, null, QoS.FireAndForget, false));
        }

        public virtual async Task Invoke(string target, object data, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Invoke(this.AsMessage(target, data, qos, retain));
        }        

        public virtual async Task Invoke(string target, IList<byte> blob, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Invoke(target, blob, string.Empty, qos, retain);
        }

        public virtual async Task Invoke(string target, byte[] data, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Invoke(target, data, string.Empty, qos, retain);
        }

        public virtual async Task Invoke(string target, IList<byte> data, object metadata, QoS qos = QoS.FireAndForget, bool retain = false)
        {
            await this.Invoke(new Message(data, metadata, target, this.ClientInfo.Controller) { QoS = qos, Retain = retain});
        }

        public virtual async Task<T> Invoke<T>(string target, int timeoutMilliseconds = 30000)
        {
            return await _Target<T>(target, timeoutMilliseconds);
        }

        public virtual async Task<T> Invoke<T>(IMessage message, int timeoutMilliseconds = 30000)
        {
            return await _Target<T>(message, timeoutMilliseconds);
        }
        public virtual async Task<T> Invoke<T>(string target, object data, int timeoutMilliseconds = 30000)
        {
            //TODO: Change to atleast once? or optional?
            return await _Target<T>(this.AsMessage(target, data, QoS.FireAndForget, false), timeoutMilliseconds);
        }

        public virtual async Task<T> Invoke<T>(string target, IList<byte> data, int timeoutMilliseconds = 30000)
        {
            return await this.Invoke<T>(new Message(data, null, target, this.ClientInfo.Controller));
        }

        public virtual async Task<T> Invoke<T>(string target, byte[] data, int timeoutMilliseconds = 2000)
        {
            return await this.Invoke<T>(new Message(data, target, this.ClientInfo.Controller));
        }

        public virtual async Task<T> Invoke<T>(string target, IList<byte> data, object metadata, int timeoutMilliseconds = 30000)
        {
            return await this.Invoke<T>(new Message(data, metadata, target, this.ClientInfo.Controller));
        }

        public virtual async Task<T> Invoke<T>(string target, byte[] data, object metadata, int timeoutMilliseconds = 30000)
        {
            return await this.Invoke<T>(new Message(data, metadata, target, this.ClientInfo.Controller));
        }

        public virtual IListener On<T>(string target, Action<T> action)
        {
            if (typeof(T) == typeof(IMessage))
            {
                var listener = new Listener(target, message => action((T)message))
                {
                    Controller = this
                };
                return this._listeners.AddOrUpdate(listener.Topic, listener);
            }            
            else
            {
                var listener = new Listener(target, message => action(this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data)))
                {
                    Controller = this
                };
                return this._listeners.AddOrUpdate(listener.Topic, listener);
            }

        }

        public virtual IListener On(string target, Action action)
        {
            var listener = new Listener(target, message => action())
            {
                Controller = this
            };
            return this._listeners.AddOrUpdate(listener.Topic, listener);
        }

        public virtual void DisposeListener(IListener listener)
        {
            this._listeners.Remove(listener.Topic.ToLower());
        }
    }
}