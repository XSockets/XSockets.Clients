using System;
using System.Collections.Generic;
using System.IO;

namespace XSockets.Client40.Protocol.FrameBuilders
{
    public class Rfc6455DataFrame
    {
        public bool IsFinal { get; set; }

        public FrameType FrameType { get; set; }

        public bool IsMasked { get; set; }


        public bool IsCompressed { get; set; }

        public long PayloadLength
        {
            get { return Payload.Length; }
        }

        public int MaskKey { get; set; }

        public byte[] Payload { get; set; }

        public byte[] ToBytes()
        {
            var memoryStream = new MemoryStream();
                      
            const bool rsv1 = false;
            const bool rsv2 = false;
            const bool rsv3 = false;

            var bt = (IsFinal ? 1 : 0) * 0x80;
            bt += (rsv1 ? 0x40 : 0x0);
            bt += (rsv2 ? 0x20 : 0x0);
            bt += (rsv3 ? 0x10 : 0x0);
            bt += (byte)FrameType;

            memoryStream.WriteByte((byte)bt);
          
            byte[] payloadLengthBytes = GetLengthBytes();

            memoryStream.Write(payloadLengthBytes, 0, payloadLengthBytes.Length);

            byte[] payload = Payload;

            if (IsMasked)
            {
                byte[] keyBytes = BitConverter.GetBytes(MaskKey);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(keyBytes);
                memoryStream.Write(keyBytes, 0, keyBytes.Length);
                payload = TransformBytes(Payload, MaskKey);
            }

            memoryStream.Write(payload, 0, Payload.Length);

            return memoryStream.ToArray();       
        }

   

        private byte[] GetLengthBytes()
        {
            var payloadLengthBytes = new List<byte>(9);

            if (PayloadLength > ushort.MaxValue)
            {
                payloadLengthBytes.Add(127);
                byte[] lengthBytes = BitConverter.GetBytes(PayloadLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);
                payloadLengthBytes.AddRange(lengthBytes);
            }
            else if (PayloadLength > 125)
            {
                payloadLengthBytes.Add(126);
                byte[] lengthBytes = BitConverter.GetBytes((UInt16) PayloadLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);
                payloadLengthBytes.AddRange(lengthBytes);
            }
            else
            {
                payloadLengthBytes.Add((byte) PayloadLength);
            }

            payloadLengthBytes[0] += (byte) (IsMasked ? 128 : 0);

            return payloadLengthBytes.ToArray();
        }

        public static byte[] TransformBytes(byte[] bytes, int mask)
        {
            var output = new byte[bytes.Length];
            byte[] maskBytes = BitConverter.GetBytes(mask);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(maskBytes);

            for (int i = 0; i < bytes.Length; i++)
            {
                output[i] = (byte) (bytes[i] ^ maskBytes[i%4]);
            }

            return output;
        }
    }
}