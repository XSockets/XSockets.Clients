using System;
using XSockets.Client40.Common.Interfaces;

namespace XSockets.Client40.Common.Event.Arguments
{
    public class OnClientConnectArgs : EventArgs
    {
        public OnClientConnectArgs(IClientInfo clientInfo)
        {
            ClientInfo = clientInfo;
        }

        #region Properties

        public IClientInfo ClientInfo { get; private set; }

        #endregion Properties
    }
}