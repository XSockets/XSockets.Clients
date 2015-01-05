using System;

namespace XSockets.ClientPortableW8.Protocol.Readers
{
    public abstract class XFrameHandler : IXFrameHandler
    {
        /// <summary>
        /// Delegate for reading frames
        /// </summary>
        public Action<ArraySegment<byte>> ReceiveData = delegate { };

        /// <summary>
        /// Will invoke the delegate for frame reading
        /// </summary>
        /// <param name="data"></param>
        public void Receive(ArraySegment<byte> data)
        {
            ReceiveData(data);
        }
    }
}