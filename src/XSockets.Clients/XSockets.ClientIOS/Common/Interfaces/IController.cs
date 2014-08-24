using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XSockets.ClientIOS.Common.Event.Arguments;
using XSockets.ClientIOS.Model;

namespace XSockets.ClientIOS.Common.Interfaces
{
    public interface IController
    {
        IClientInfo ClientInfo { get; set; }
        IXSocketClient XSocketClient { get; }

        event EventHandler<Message> OnMessage;
        event EventHandler<OnClientConnectArgs> OnOpen;
        event EventHandler<OnClientDisconnectArgs> OnClose;
        event EventHandler<OnErrorArgs> OnError;
        
        void Close();        

        void BindUnboundSubscriptions();

        void FireOnMessage(IMessage message);
        void FireOnBlob(IMessage message);

        IMessage AsMessage(string topic, object o);

        //Properties
        void SetEnum(string propertyName, string value);
        void SetProperty(string propertyName, object value);

        //STORAGE
        T StorageGet<T>(string key);
        void StorageSet<T>(string key, T value) where T : class;
        IListener StorageOnSet<T>(string key, Action<T> action) where T : class;
        IListener StorageOnRemove<T>(string key, Action<T> action) where T : class;
        void StorageRemove(string key);
        void StorageClear();


        //PUBSUB
        void One(string topic, Action<IMessage> callback);
        void One<T>(string topic, Action<T> callback) where T : class;
        void One(string topic, Action<IMessage> callback, Action<IMessage> confirmCallback);
        void One<T>(string topic, Action<T> callback, Action<IMessage> confirmCallback) where T : class;
        void Many(string topic, uint limit, Action<IMessage> callback);
        void Many<T>(string topic, uint limit, Action<T> callback) where T : class;
        void Many(string topic, uint limit, Action<IMessage> callback, Action<IMessage> confirmCallback);
        void Many<T>(string topic, uint limit, Action<T> callback, Action<IMessage> confirmCallback) where T : class;

        /// <summary>
        /// Remove the subscription from the list
        /// </summary>
        /// <param name="topic"></param>
        void Unsubscribe(string topic);

        void Subscribe(string topic);
        void Subscribe(string topic, SubscriptionType subscriptionType, uint limit = 0);
        void Subscribe(string topic, Action<IMessage> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0);
        void Subscribe(string topic, Action<IMessage> callback, Action<IMessage> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0);
        void Subscribe<T>(string topic, Action<T> callback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0) where T : class;
        void Subscribe<T>(string topic, Action<T> callback, Action<IMessage> confirmCallback, SubscriptionType subscriptionType = SubscriptionType.All, uint limit = 0) where T : class;

        /// <summary>
        ///     Send message
        /// </summary>
        /// <param name="payload">IMessage</param>
        /// <param name="callback"> </param>
        void Publish(IMessage payload, Action callback);

        /// <summary>
        /// Send a binary message with metadata
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="data"></param>
        /// <param name="metadata"></param>                
        void Publish(string topic, List<byte> data, object metadata);
        void Publish(string topic, byte[] data, object metadata);
        void Publish(string topic, List<byte> data);
        void Publish(string topic, byte[] data);

        void Publish(string payload);
        void Publish(string payload, Action callback);
        void Publish(IMessage payload);
        void Publish(string topic, object obj);
        void Publish(string topic, object obj, Action callback);        

        //RPC
        void Invoke(IMessage payload);
        void Invoke(string target); 
        void Invoke(string target, object data);
        void Invoke(string target, IList<byte> data);
        void Invoke(string target, byte[] data);
        void Invoke(string target, IList<byte> data, object metadata);
        Task<T> Invoke<T>(string target, int timeoutMilliseconds = 30000);
        Task<T> Invoke<T>(string target, object data, int timeoutMilliseconds = 30000);
        Task<T> Invoke<T>(string target, IList<byte> data, int timeoutMilliseconds = 30000);
        Task<T> Invoke<T>(string target, byte[] data, int timeoutMilliseconds = 30000);
        Task<T> Invoke<T>(string target, IList<byte> blob, object metadata, int timeoutMilliseconds = 30000);
        Task<T> Invoke<T>(string target, byte[] blob, object metadata, int timeoutMilliseconds = 30000);
        IListener On<T>(string target, Action<T> action);
        //IListener On(string target, Action<dynamic> action);
        IListener On(string target, Action action);
        void DisposeListener(IListener listener);
    }
}