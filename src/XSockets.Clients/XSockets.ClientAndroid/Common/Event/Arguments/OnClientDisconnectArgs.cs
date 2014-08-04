using System;
using XSockets.ClientAndroid.Common.Interfaces;

namespace XSockets.ClientAndroid.Common.Event.Arguments
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