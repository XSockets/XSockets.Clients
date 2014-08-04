using System.Collections.Generic;

namespace XSockets.Client40.Protocol.Readers
{
    public class ReadState : IReadState
    {
        public ReadState()
        {
            Data = new List<byte>();
        }

        public List<byte> Data { get; private set; }
        public FrameType? FrameType { get; set; }

        public void Clear()
        {
            Data.Clear();
            FrameType = null;
        }
    }
}