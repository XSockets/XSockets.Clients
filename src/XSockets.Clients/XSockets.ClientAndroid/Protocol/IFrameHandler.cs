using System.Collections.Generic;

namespace XSockets.ClientAndroid.Protocol
{
    public interface IXFrameHandler
    {
        void Receive();
        List<byte> Data { get; set; }
    }
}