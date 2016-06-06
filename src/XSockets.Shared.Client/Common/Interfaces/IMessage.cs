
namespace XSockets.Common.Interfaces
{
    using System.Collections.Generic;
    using Event.Arguments;
    using Model;

    public interface IMessage
    {        
        int Id { get; set; }
        QoS QoS { get; set; }        
        bool Retain { get; set; }
        IEnumerable<byte> Blob { get; }
        string Data { get; }
        MessageType MessageType { get; }
        string Controller { get; set; }
        string Topic { get; set; }
        T Extract<T>();
        string ToString();
        byte[] ToBytes();
    }
}