using System.Collections.Generic;

namespace XSockets.Client35.Protocol
{
    public interface IXFrameHandler
    {
        void Receive(IEnumerable<byte> data);
    }
}