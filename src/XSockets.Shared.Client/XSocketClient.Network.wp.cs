#if WINDOWS_PHONE_APP || WINDOWS_UWP

namespace XSockets
{
    using System.Net.NetworkInformation;
    using XSockets.Common.Interfaces;
    using Windows.Networking.Connectivity; 

    public partial class XSocketClient : IXSocketClient
    {                       
        private NetworkState GetNetworkState()
        {            
            try
            {            
                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                var online = (connectionProfile != null &&
                                 connectionProfile.GetNetworkConnectivityLevel() ==
                                 NetworkConnectivityLevel.InternetAccess);

                return online ? NetworkState.Online : NetworkState.Offline;                   
            }
            catch
            {
                return NetworkState.Unknown;
            }
        }
        public void UpdateWpNetworkStatus()
        {
            _state = GetNetworkState();
        }

        public void WpNetworkWatcher()
        {            
            _state = NetworkState.Unknown;
            _state = GetNetworkState(); 
            NetworkChange.NetworkAddressChanged += (s, e) => 
            {
                var currentState = _state;
                _state = GetNetworkState();

                if (currentState != _state)
                {
                    if (currentState != _state && OnNetworkStatusChanged != null)
                    {
                        OnNetworkStatusChanged(this, _state);
                    }
                }
            };
            NetworkInformation.NetworkStatusChanged += (s)=>
            {
                var currentState = _state;
                _state = GetNetworkState();                
                if (currentState != _state)
                {
                    if(this.Communication != null)
                        this.Communication.UpdateNetworkState(_state);
                    if (OnNetworkStatusChanged != null)
                    {
                        OnNetworkStatusChanged(this, _state);
                    }
                }
            };            
        } 
    }      
}
#endif