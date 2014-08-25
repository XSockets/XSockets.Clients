using System.Collections.Generic;

namespace XSockets.Client35.Protocol
{
    public interface IXFrameHandler
    {
        List<byte> Data { get; set; }
        void Receive();
    }
}