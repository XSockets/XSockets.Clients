using System;
using XSockets.Common.Interfaces;

namespace XSockets.Common.Event.Arguments
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