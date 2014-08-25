using System.Collections.Generic;

namespace XSockets.ClientIOS.Protocol
{
    public interface IXFrameHandler
    {
        void Receive();
        List<byte> Data { get; set; }
    }
}