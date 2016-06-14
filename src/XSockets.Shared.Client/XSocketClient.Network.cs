
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
#if __ANDROID__
            AndroidNetworkWatcher();
#elif __IOS__            
            iOSNetworkWatcher();
#elif WINDOWS_UWP || WINDOWS_PHONE_APP
            WpNetworkWatcher();
#else            
            DefaultNetworkWatcher();
#endif
        }
        public NetworkState NetworkState
        {
            get
            {
#if __ANDROID__
                UpdateAndroidNetworkStatus();
#elif __IOS__
                UpdateIOSNetworkStatus();
#elif WINDOWS_PHONE_APP || WINDOWS_UWP
                UpdateWpNetworkStatus();
#endif
                return _state;
            }
        }
    }

}