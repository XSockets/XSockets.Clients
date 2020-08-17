
namespace XSockets
{
    using System;
    using Common.Interfaces;

    public enum NetworkState
    {
        Unknown,
        Connecting,
        Online,
        Offline
    }

    public partial class XSocketClient : IXSocketClient
    {
        public event EventHandler<NetworkState> OnNetworkStatusChanged;

        private NetworkState _state;

        public void NetworkWatcher()
        {
            DefaultNetworkWatcher();
        }
        public NetworkState NetworkState
        {
            get
            {
                return _state;
            }
        }
    }

}