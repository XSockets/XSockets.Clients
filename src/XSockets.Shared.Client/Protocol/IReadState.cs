
namespace XSockets.Protocol
{
    using System.Collections.Generic;

    public interface IReadState
    {
        List<byte> Data { get; }
        FrameType? FrameType { get; set; }
        void Clear();
    }
}