using System;
using XSockets.Common.Interfaces;

namespace XSockets.Common.Event.Arguments
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