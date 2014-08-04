using System.Collections.Concurrent;
using System.Threading.Tasks;
using XSockets.ClientAndroid.Common.Interfaces;
using XSockets.ClientAndroid.Helpers;
using XSockets.ClientAndroid.Model;
using XSockets.ClientAndroid.Utility.Storage;

namespace XSockets.ClientAndroid
{
    /// <summary>    
    /// Acts as a wrapper and abstraction for XSocketClient.
    /// If you are only publishing this is the class to use
    /// </summary>
    public class ClientPool
    {
        private BlockingCollection<IMessage> _textQueue;

        private IXSocketClient _websocket;

        private IXSocketJsonSerializer _jsonSerializer;

        private string _conn;
        private static readonly object Locker = new object();

        public static ClientPool GetInstance(string conn, string origin)
        {
            lock (Locker)
            {
                //Safety, remove disconnected instances
                Repository<string, ClientPool>.Remove(p => !p._websocket.Socket.Connected);
                if (!Repository<string, ClientPool>.ContainsKey(conn))
                {
                    var x = new ClientPool
                    {
                        _conn = conn,
                        _textQueue = new BlockingCollection<IMessage>(),
                        _jsonSerializer = new XSocketJsonSerializer()
                    };
                    x._websocket = new XSocketClient(x._conn, origin);
                    ((XSocketClient)x._websocket).OnConnected += (sender, args) => Task.Factory.StartNew(() =>
                    {
                        //Will send messages to the XSockets server as soon as there is messages in the queue.
                        foreach (var v in x._textQueue.GetConsumingEnumerable())
                        {
                            Repository<string, ClientPool>.GetById(x._conn)._websocket.Controller(v.Controller).Publish(v);
                        }
                    });
                    x._websocket.OnDisconnected += (sender, args) => Repository<string, ClientPool>.Remove(x._conn);                    
                    Repository<string, ClientPool>.AddOrUpdate(conn, x);
                }
            }
            return Repository<string, ClientPool>.GetById(conn);
        }

        /// <summary>
        /// Send prepared IMessage
        /// </summary>
        /// <param name="message"></param>
        public void Send(IMessage message)
        {
            Repository<string, ClientPool>.GetById(_conn)._textQueue.Add(message);
        }

        /// <summary>
        /// Send any object, with an eventname... will be transformed into a IMessage
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="eventname"></param>
        public void Send(object obj, string eventname, string controller = null)
        {
            Send(new Message(obj, eventname, controller, _jsonSerializer));
        }
    }
}
