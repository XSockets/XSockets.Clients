using System.Collections.Generic;

namespace XSockets.ClientAndroid.Protocol
{
    public interface IXFrameHandler
    {
        void Receive(IEnumerable<byte> data);
    }
}