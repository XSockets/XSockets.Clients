using System;

namespace XSockets.ClientPortableW8.Protocol
{
    public interface IXFrameHandler
    {
        void Receive(ArraySegment<byte> data);        
    }
}