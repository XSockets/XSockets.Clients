using System.Net.Sockets;
using Json.NETMF;
using XSockets.ClientMF43.Event;

namespace XSockets.ClientMF43.Interfaces
{
    public interface IXSocketClient
    {
        Socket Socket { get; }
        string Handshake { get; }        
        string Server { get; }
        int Port { get; }
        string ProtocolName { get; set; }
        string ProtocolResponse { get; set; }
        JsonSerializer Serializer { get; set; }
        event EventHandler OnOpen;
        event EventHandler OnClose;
        event EventHandler OnError;
        event MessageHandler OnMessage;
        void Open();
        void Close();        
        void Publish(string topic, object data, string controller);        
        void Subscribe(string @event, string controller);        
        void Unsubscribe(string @event, string controller);
        void Recieve();
        void Dispose();
        void SetEnum(string propertyName, string value, string controller);
        void SetProperty(string propertyName, object value, string controller);
    }
}
