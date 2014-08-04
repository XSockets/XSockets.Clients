using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using XSockets.Client35.Common.Event.Arguments;
using XSockets.Client35.Common.Interfaces;
using XSockets.Client35.Model;

namespace XSockets.Client35
{
    public partial class Controller : IController
    {
        private Task<T> _Target<T>(IMessage m, int timeoutMilliseconds)
        {
            return new Task<T>(() =>
            {
                var data = default(T);
                var working = true;
                var listener = new Listener(m.Topic,
                    message =>
                    {
                        Console.WriteLine(message.Data);
                        data = this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data);
                        working = false;
                    },
                    SubscriptionType.One);

                this.Listeners.AddOrUpdate(m.Topic, listener);

                this.Invoke(m);
                var r = SpinWait.SpinUntil(() => working == false, timeoutMilliseconds);

                if (r == false)
                {
                    this.DisposeListener(listener);
                    throw new TimeoutException("The server did not respond in the given time frame");
                }

                return data;
            });
        }

        private Task<T> _Target<T>(string s, int timeoutMilliseconds)
        {
            s = s.ToLower();
            return new Task<T>(() =>
            {
                var data = default(T);
                var working = true;
                var listener = new Listener(s,
                    message =>
                    {
                        data = this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data); working = false;
                    },
                    SubscriptionType.One);

                this.Listeners.AddOrUpdate(s, listener);

                this.Invoke(this.AsMessage(s, null));

                var r = SpinWait.SpinUntil(() => working == false, timeoutMilliseconds);

                if (r == false)
                {
                    this.DisposeListener(listener);
                    throw new TimeoutException("The server did not respond in the given time frame");
                }

                return data;
            });
        }

        public void Invoke(IMessage payload)
        {
            if (!this.XSocketClient.IsConnected)
                throw new Exception("You cant send messages when not connected to the server");
            payload.Controller = this.ClientInfo.Controller;
            var frame = GetDataFrame(payload);
            this.XSocketClient.Socket.Send(frame.ToBytes(), () => { }, err => FireClosed());
        }

        public void Invoke(string target)
        {
            this.Invoke(this.AsMessage(target, null));
        }

        public void Invoke(string target, object data)
        {
            this.Invoke(this.AsMessage(target, data));
        }

        public void Invoke(string target, IList<byte> blob)
        {
            this.Invoke(new Message(blob, MessageType.Binary));
        }

        public T Invoke<T>(string target, int timeoutMilliseconds = 2000)
        {
            var waiter = _Target<T>(target, timeoutMilliseconds);
            waiter.Start();

            return TaskCompletionHandlerResult<T>(waiter);
        }

        public T Invoke<T>(IMessage message, int timeoutMilliseconds = 2000)
        {
            var waiter = _Target<T>(message, timeoutMilliseconds);
            waiter.Start();
            return TaskCompletionHandlerResult<T>(waiter);
        }
        public T Invoke<T>(string target, object data, int timeoutMilliseconds = 2000)
        {
            var waiter = _Target<T>(this.AsMessage(target, data), timeoutMilliseconds);
            waiter.Start();
            return TaskCompletionHandlerResult<T>(waiter);
        }

        public T Invoke<T>(string target, IList<byte> data, int timeoutMilliseconds = 2000)
        {
            return this.Invoke<T>(target, new Message(data, MessageType.Binary), timeoutMilliseconds);
        }

        public T Invoke<T>(string target, byte[] data, int timeoutMilliseconds = 2000)
        {
            return this.Invoke<T>(target, new Message(data, target, this.ClientInfo.Controller), timeoutMilliseconds);
        }

        public T Invoke<T>(string target, IList<byte> data, object metadata, int timeoutMilliseconds = 2000)
        {
            return this.Invoke<T>(target, new Message(data, metadata, target, this.ClientInfo.Controller), timeoutMilliseconds);
        }

        public T Invoke<T>(string target, byte[] data, object metadata, int timeoutMilliseconds = 2000)
        {
            return this.Invoke<T>(target, new Message(data, metadata, target, this.ClientInfo.Controller), timeoutMilliseconds);
        }

        private static T TaskCompletionHandlerResult<T>(Task<T> waiter)
        {
            var tcs = new TaskCompletionSource<T>();

            waiter.ContinueWith(t => t.Exception.Handle(ex =>
            {
                tcs.TrySetException(ex);
                return false;
            }), TaskContinuationOptions.OnlyOnFaulted);

            waiter.ContinueWith(task => { tcs.SetResult(waiter.Result); }, TaskContinuationOptions.OnlyOnRanToCompletion);

            return tcs.Task.Result;
        }

        public IListener On<T>(string target, Action<T> action)
        {
            if (typeof(T) == typeof(IMessage))
            {
                var listener = new Listener(target, message => action((T)message))
                {
                    Controller = this
                };
                return this.Listeners.AddOrUpdate(listener.Topic, listener);
            }
            else
            {
                var listener = new Listener(target, message => action(this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data)))
                {
                    Controller = this
                };
                return this.Listeners.AddOrUpdate(listener.Topic, listener);
            }

        }

        //public IListener On(string target, Action<dynamic> action)
        //{
        //    var listener = new Listener(target, message => action(this.XSocketClient.Serializer.DeserializeFromString(message.Data)))
        //    {
        //        Controller = this
        //    };
        //    return this.Listeners.AddOrUpdate(listener.Topic, listener);
        //}

        public IListener On(string target, Action action)
        {
            var listener = new Listener(target, message => action())
            {
                Controller = this
            };
            return this.Listeners.AddOrUpdate(listener.Topic, listener);
        }

        public void DisposeListener(IListener listener)
        {
            this.Listeners.Remove(listener.Topic);
        }


        public void Invoke(string target, byte[] data)
        {
            this.Invoke(target, data, "");
        }

        public void Invoke(string target, IList<byte> data, object metadata)
        {
            this.Invoke(new Message(data, metadata, target, this.ClientInfo.Controller));
        }
    }
    //public partial class Controller : IController
    //{
    //    private Task<T> _Target<T>(IMessage m, int timeoutMilliseconds)
    //    {
    //        return new Task<T>(() =>
    //        {
    //            var data = default(T);
    //            var working = true;
    //            var listener = new Listener(m.Topic,
    //                message =>
    //                {
    //                    Console.WriteLine(message.Data);
    //                    data = this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data);
    //                    working = false;
    //                },
    //                SubscriptionType.One);

    //            this.Listeners.AddOrUpdate(m.Topic, listener);

    //            this.Invoke(m);
    //            var r = SpinWait.SpinUntil(() => working == false, timeoutMilliseconds);

    //            if (r == false)
    //                throw new TimeoutException("The server did not respond in the given time frame");

    //            return data;
    //        });
    //    }

    //    private Task<T> _Target<T>(string s, int timeoutMilliseconds)
    //    {
    //        s = s.ToLower();
    //        return new Task<T>(() =>
    //        {
    //            var data = default(T);
    //            var working = true;
    //            var listener = new Listener(s,
    //                message =>
    //                {
    //                    data = this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data); working = false;
    //                },
    //                SubscriptionType.One);

    //            this.Listeners.AddOrUpdate(s, listener);

    //            this.Invoke(this.AsMessage(s, null));

    //            var r = SpinWait.SpinUntil(() => working == false, timeoutMilliseconds);

    //            if (r == false)
    //                throw new TimeoutException("The server did not respond in the given time frame");

    //            return data;
    //        });
    //    }

    //    public void Invoke(IMessage payload)
    //    {
    //        if (!this.XSocketClient.IsConnected)
    //            throw new Exception("You cant send messages when not connected to the server");
    //        payload.Controller = this.ClientInfo.Controller;
    //        var frame = GetDataFrame(payload);
    //        this.XSocketClient.Socket.Send(frame.ToBytes(), () => { }, err => FireClosed());
    //    }

    //    public void Invoke(string target)
    //    {
    //        this.Invoke(this.AsMessage(target, null));
    //    }

    //    public void Invoke(string target, object data)
    //    {
    //        this.Invoke(this.AsMessage(target, data));
    //    }

    //    public void Invoke(string target, IList<byte> blob)
    //    {
    //        this.Invoke(new Message(blob, MessageType.Binary));
    //    }

    //    public Task<T> Invoke<T>(string target)
    //    {
    //        var waiter = _Target<T>(target);
    //        waiter.Start();
    //        return waiter;
    //    }

    //    public Task<T> Invoke<T>(IMessage message)
    //    {
    //        var waiter = _Target<T>(message);
    //        waiter.Start();
    //        return waiter;
    //    }
    //    public Task<T> Invoke<T>(string target, object data)
    //    {
    //        var waiter = _Target<T>(this.AsMessage(target, data));
    //        waiter.Start();
    //        return waiter;
    //    }

    //    public Task<T> Invoke<T>(string target, IList<byte> data)
    //    {
    //        return this.Invoke<T>(target, new Message(data, MessageType.Binary));
    //    }

    //    public Task<T> Invoke<T>(string target, byte[] data)
    //    {
    //        return this.Invoke<T>(target, new Message(data, target, this.ClientInfo.Controller));
    //    }

    //    public Task<T> Invoke<T>(string target, IList<byte> data, object metadata)
    //    {
    //        return this.Invoke<T>(target, new Message(data, metadata, target, this.ClientInfo.Controller));
    //    }

    //    public Task<T> Invoke<T>(string target, byte[] data, object metadata)
    //    {
    //        return this.Invoke<T>(target, new Message(data, metadata, target, this.ClientInfo.Controller));
    //    }

    //    public IListener On<T>(string target, Action<T> action)
    //    {
    //        if (typeof(T) == typeof(IMessage))
    //        {
    //            var listener = new Listener(target, message => action((T)message))
    //            {
    //                Controller = this
    //            };
    //            return this.Listeners.AddOrUpdate(listener.Topic, listener);
    //        }
    //        else
    //        {
    //            var listener = new Listener(target, message => action(this.XSocketClient.Serializer.DeserializeFromString<T>(message.Data)))
    //            {
    //                Controller = this
    //            };
    //            return this.Listeners.AddOrUpdate(listener.Topic, listener);
    //        }

    //    }
        
    //    public IListener On(string target, Action action)
    //    {
    //        var listener = new Listener(target, message => action())
    //        {
    //            Controller = this
    //        };
    //        return this.Listeners.AddOrUpdate(listener.Topic, listener);
    //    }

    //    public void DisposeListener(IListener listener)
    //    {
    //        this.Listeners.Remove(listener.Topic);
    //    }


    //    public void Invoke(string target, byte[] data)
    //    {
    //        this.Invoke(target, data, "");
    //    }

    //    public void Invoke(string target, IList<byte> data, object metadata)
    //    {
    //        this.Invoke(new Message(data, metadata, target, this.ClientInfo.Controller));
    //    }
    //}
}