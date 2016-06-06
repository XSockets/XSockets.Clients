
namespace XSockets
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using XSockets.Common.Interfaces;
    using XSockets.Helpers;
    using XSockets.Model;
    using XSockets.Utility.Storage;

    /// <summary>    
    /// Acts as a wrapper and abstraction for XSocketClient.
    /// If you are only publishing/sending this is the class to use
    /// </summary>
    public class ClientPool
    {
        private BlockingCollection<IMessage> _textQueue;

        private IXSocketClient _websocket;

        private IXSocketJsonSerializer _jsonSerializer;

        private string _conn;
        private string _port;
        private IDictionary<string, string> _querystringParameters;
        private static readonly object Locker = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn">server location ws://server</param>
        /// <param name="port">the port to use, for example 8080</param>
        /// <param name="origin">the origin of this clients, http://example.com</param>
        /// <param name="Querystring parameters">Parameters to pass in with the connection</param>
        /// <returns></returns>
        public static ClientPool GetInstance(string conn, string port, string origin, IDictionary<string,string> querystringParameters = null)
        {
            lock (Locker)
            {
                //Safety, remove disconnected instances
                Repository<string, ClientPool>.Remove(p => !p._websocket.Communication.Connected);
                if (!Repository<string, ClientPool>.ContainsKey(conn+port))
                {
                    var x = new ClientPool
                    {
                        _conn = conn,
                        _port = port,
                        _querystringParameters = querystringParameters,
                        _textQueue = new BlockingCollection<IMessage>(),
                        _jsonSerializer = new XSocketJsonSerializer()
                    };
                    x._websocket = new XSocketClient(string.Format("{0}:{1}",x._conn, x._port), origin);
                    if(x._querystringParameters != null)
                        foreach(var q in x._querystringParameters)
                            x._websocket.QueryString.Add(q.Key, q.Value);
                    ((XSocketClient)x._websocket).OnConnected += (sender, args) => Task.Factory.StartNew(() =>
                    {
                        //Will send messages to the XSockets server as soon as there is messages in the queue.
                        foreach (var v in x._textQueue.GetConsumingEnumerable())
                        {
                            var ctrl =
                                Repository<string, ClientPool>.GetById(x._conn+x._port)._websocket.Controller(v.Controller);
                            if (ctrl.ClientInfo.ConnectionId == Guid.Empty)
                            {
                                ctrl.ClientInfo.ConnectionId = Guid.NewGuid();
                            }
                            ctrl.Publish(v);
                        }
                    });
                    
                    x._websocket.OnDisconnected += (sender, args) => Repository<string, ClientPool>.Remove(x._conn+port);
                    x._websocket.Open();
                    Repository<string, ClientPool>.AddOrUpdate(conn + port, x);
                }
            }
            return Repository<string, ClientPool>.GetById(conn+port);
        }

        /// <summary>
        /// Send prepared IMessage
        /// </summary>
        /// <param name="message"></param>
        public void Send(IMessage message)
        {
            Repository<string, ClientPool>.GetById(_conn+_port)._textQueue.Add(message);
        }

        /// <summary>
        /// Send any object, with an eventname... will be transformed into a IMessage
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="eventname"></param>
        /// <param name="controller"></param>
        public void Send(object obj, string eventname, string controller)
        {
            Send(new Message(obj, eventname, controller, _jsonSerializer));
        }
    }
}
