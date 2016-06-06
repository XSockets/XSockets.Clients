using System.Collections.Generic;

namespace XSockets.Protocol
{
    public interface IXFrameHandler
    {
        void Receive();
        List<byte> Data { get; set; }
    }
}