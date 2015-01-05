using System;
using XSockets.ClientPortableW8.Common.Interfaces;

namespace XSockets.ClientPortableW8.Common.Event.Arguments
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