
namespace XSockets.Protocol.Readers
{
    using System.Collections.Generic;

    /// <summary>
    /// Readstate when reading frames
    /// </summary>
    public class ReadState : IReadState
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public ReadState()
        {
            Data = new List<byte>();
            FrameBytes = new List<byte>();
        }

        #region IReadState Members
        /// <summary>
        /// 
        /// </summary>
        public List<byte> Data { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public List<byte> FrameBytes { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public FrameType? FrameType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int BufferedIndex { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsFinal { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public virtual void Clear()
        {
            this.FrameType = null;
            this.FrameBytes.Clear();
            this.IsFinal = false;
            this.Length = 0;
            this.BufferedIndex = 0;
        }

        #endregion
    }
}