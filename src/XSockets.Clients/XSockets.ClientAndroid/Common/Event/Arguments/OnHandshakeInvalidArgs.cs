using System;
using XSockets.ClientAndroid.Common.Interfaces;

namespace XSockets.ClientAndroid.Common.Event.Arguments
{
    public class OnHandshakeInvalidArgs : EventArgs
    {
        public OnHandshakeInvalidArgs(IXSocketController controller, string handshake)
        {
            Controller = controller;
            Handshake = handshake;
        }

        #region Properties

        public IXSocketController Controller { get; private set; }
        public string Handshake { get; private set; }

        #endregion Properties
    }
}