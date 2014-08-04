using System;
using XSockets.ClientAndroid.Common.Interfaces;

namespace XSockets.ClientAndroid.Common.Event.Arguments
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