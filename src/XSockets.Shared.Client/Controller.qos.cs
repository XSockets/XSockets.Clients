namespace XSockets
{
    using System;
    using System.Threading;
    using XSockets.Common.Interfaces;

    public partial class Controller : IController
    {
        private int _qosId;
        public int GetIdForQoS()
        {            
            if (_qosId >= UInt16.MaxValue)
                _qosId = UInt16.MinValue;
            Interlocked.Increment(ref _qosId);
            return _qosId;
        }
    }
}