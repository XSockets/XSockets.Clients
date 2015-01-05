using System.Collections.Generic;

namespace XSockets.ClientPortableW8.Protocol
{
    public interface IReadState
    {
        List<byte> Data { get; }
        FrameType? FrameType { get; set; }
        void Clear();
    }
}