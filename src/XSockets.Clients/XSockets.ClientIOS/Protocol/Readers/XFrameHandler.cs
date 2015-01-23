using System;
using System.Collections.Generic;

namespace XSockets.ClientIOS.Protocol.Readers
{
    public abstract class XFrameHandler : IXFrameHandler
    {
        public List<byte> Data { get; set; }

        public Action<List<byte>> ReceiveData = delegate { };

        public void Receive()
        {
            ReceiveData(Data);
        }

        protected XFrameHandler()
        {
            this.Data = new List<byte>();
        }
    }
}