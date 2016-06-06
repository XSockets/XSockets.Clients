#if __IOS__

namespace XSockets
{
    using System.Net;
    using XSockets.Common.Interfaces;
    using SystemConfiguration;
    using CoreFoundation;

    public partial class XSocketClient : IXSocketClient
    {
        private static NetworkReachability _defaultRouteReachability;
        
        public void iOSNetworkWatcher()
        {            
            if (_defaultRouteReachability == null)
            {
                _defaultRouteReachability = new NetworkReachability(new IPAddress(0));
                _defaultRouteReachability.SetNotification(NetworkStatusChanged);
                _defaultRouteReachability.Schedule(CFRunLoop.Current, CFRunLoop.ModeDefault);
            }

            UpdateIOSNetworkStatus();
        }

        public void UpdateIOSNetworkStatus()
        {
            _state = NetworkState.Unknown;

            NetworkReachabilityFlags flags;

            if (_defaultRouteReachability.TryGetFlags(out flags) &&
                IsReachableWithoutRequiringConnection(flags))
                _state = NetworkState.Online;
            else
                _state = NetworkState.Offline;            
        }

        private static bool IsReachableWithoutRequiringConnection(NetworkReachabilityFlags flags)
        {
            // Is it reachable with the current network configuration?
            bool isReachable = (flags & NetworkReachabilityFlags.Reachable) != 0;

            // Do we need a connection to reach it?
            bool noConnectionRequired = (flags & NetworkReachabilityFlags.ConnectionRequired) == 0;

            // Since the network stack will automatically try to get the WAN up,
            // probe that
            if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
                noConnectionRequired = true;

            return isReachable && noConnectionRequired;
        }

        private void NetworkStatusChanged(NetworkReachabilityFlags flags)
        {
            var currentState = _state;
            UpdateIOSNetworkStatus();            

            if (currentState != _state)
            {
                if(this.Communication != null)
                        this.Communication.UpdateNetworkState(_state);
                if (currentState != _state && OnNetworkStatusChanged != null)
                {
                    OnNetworkStatusChanged(this, _state);
                }
            }
        }
    }
}
#endif