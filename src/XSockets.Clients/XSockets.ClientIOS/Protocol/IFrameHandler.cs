using System.Collections.Generic;

namespace XSockets.ClientIOS.Protocol
{
    public interface IXFrameHandler
    {
        void Receive(IEnumerable<byte> data);
    }
}