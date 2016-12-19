#if __ANDROID__

namespace XSockets
{
    using Android.App;
    using Android.Content;
    using Android.Net;
    using System;
    using XSockets.Common.Interfaces;

    public partial class XSocketClient : IXSocketClient
    {
        private NetworkStatusBroadcastReceiver _broadcastReceiver;

        public void UpdateAndroidNetworkStatus()
        {
            var currentState = _state;
            _state = NetworkState.Unknown;

            // Retrieve the connectivity manager service
            var connectivityManager = (ConnectivityManager)Application.Context.GetSystemService(Context.ConnectivityService);

            // Check if the network is connected or connecting.
            // This means that it will be available,
            // or become available in a few seconds.
            var activeNetworkInfo = connectivityManager.ActiveNetworkInfo;

            if (activeNetworkInfo != null && activeNetworkInfo.IsConnected)
            {
                _state = NetworkState.Online;
            }
            else if (activeNetworkInfo != null && activeNetworkInfo.IsConnectedOrConnecting)
            {
                _state = NetworkState.Connecting;
            }
            else
            {
                _state = NetworkState.Offline;
            }
            
            //Check if the state has changed.
            //if it has we want to update the network state and disconnect if state is now offline
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

        public void AndroidNetworkWatcher()
        {

            if (_broadcastReceiver != null)
            {
                throw new InvalidOperationException(
                    "Network status monitoring already active.");
            }

            // Create the broadcast receiver and bind the event handler
            // so that the app gets updates of the network connectivity status
            _broadcastReceiver = new NetworkStatusBroadcastReceiver();
            _broadcastReceiver.ConnectionStatusChanged += NetworkStatusChanged;

            // Register the broadcast receiver
            Application.Context.RegisterReceiver(_broadcastReceiver,
                new IntentFilter(ConnectivityManager.ConnectivityAction));

        }

        void NetworkStatusChanged(object sender, EventArgs e)
        {
            UpdateAndroidNetworkStatus();              
        }
    }

    [BroadcastReceiver()]
    public class NetworkStatusBroadcastReceiver : BroadcastReceiver
    {
        public event EventHandler ConnectionStatusChanged;

        public override void OnReceive(Context context, Intent intent)
        {
            if (ConnectionStatusChanged != null)
                ConnectionStatusChanged(this, EventArgs.Empty);
        }
    }
}
#endif
