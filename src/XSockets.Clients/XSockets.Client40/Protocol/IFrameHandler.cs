using System.Collections.Generic;

namespace XSockets.Client40.Protocol
{
    public interface IXFrameHandler
    {
        void Receive();
        List<byte> Data { get; set; }
    }
}