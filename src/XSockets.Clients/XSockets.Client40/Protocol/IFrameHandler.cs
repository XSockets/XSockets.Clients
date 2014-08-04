using System.Collections.Generic;

namespace XSockets.Client40.Protocol
{
    public interface IXFrameHandler
    {
        void Receive(IEnumerable<byte> data);
    }
}