using System;
using XSockets.ClientIOS.Common.Interfaces;

namespace XSockets.ClientIOS.Common.Event.Arguments
{
    public class OnHandshakeCompleteArgs : EventArgs
    {
        public OnHandshakeCompleteArgs(IXSocketController controller, string handshake)
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