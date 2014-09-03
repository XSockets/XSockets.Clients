using System;
using System.Text;

namespace XSockets.ClientMF42.Model
{
    /// <summary>
    /// Internal data frames
    /// </summary>
    public class XDataFrame
    {
        public string Payload { get; set; }

        public XDataFrame() { }

        public XDataFrame(string payload)
        {
            this.Payload = payload;
        }

        public byte[] ToBytes()
        {
            return Wrapper(Encoding.UTF8.GetBytes(Payload));
        }

        public byte[] Wrapper(byte[] bytes)
        {
            var wrappedBytes = new byte[bytes.Length + 2];
            wrappedBytes[0] = 0x00;
            wrappedBytes[wrappedBytes.Length - 1] = 0xff;
            Array.Copy(bytes, 0, wrappedBytes, 1, bytes.Length);
            return wrappedBytes;
        }
    }
}
