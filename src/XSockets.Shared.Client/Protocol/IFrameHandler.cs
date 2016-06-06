
namespace XSockets.Protocol
{
    using System;

    public interface IXFrameHandler
    {
        void Receive(ArraySegment<byte> data);        
    }
}