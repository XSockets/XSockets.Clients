using System;
using XSockets.Client35.Common.Interfaces;

namespace XSockets.Client35.Common.Event.Arguments
{
    public class OnClientDisconnectArgs : EventArgs
    {
        public OnClientDisconnectArgs(IClientInfo clientInfo)
        {
            ClientInfo = clientInfo;
        }

        #region Properties

        public IClientInfo ClientInfo { get; private set; }

        #endregion Properties
    }
}