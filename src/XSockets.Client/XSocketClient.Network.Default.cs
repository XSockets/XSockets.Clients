﻿namespace XSockets
{
    using System.Net.NetworkInformation;
    using Common.Interfaces;

    public partial class XSocketClient : IXSocketClient
    {
        private NetworkState GetNetworkState()
        {
            try
            {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                NetworkInterface[] interfaces =
                    NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface face in interfaces)
                {
                    if (face.OperationalStatus == OperationalStatus.Up)
                    {
                        if ((face.NetworkInterfaceType != NetworkInterfaceType.Tunnel) &&
                            (face.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                        {
                            IPv4InterfaceStatistics statistics =
                                face.GetIPv4Statistics();
                            if ((statistics.BytesReceived > 0) &&
                                (statistics.BytesSent > 0))
                            {
                                return NetworkState.Online;
                            }
                        }
                    }
                }
            }

            return NetworkState.Offline;
            }
            catch
            {
                return NetworkState.Unknown;
            }
        }

        public void UpdateDefaultNetworkStatus()
        {
            _state = GetNetworkState();
        }

        public void DefaultNetworkWatcher()
        {
            _state = GetNetworkState();

            NetworkChange.NetworkAddressChanged += async (s, e) =>
            {
                var currentState = _state;
                _state = GetNetworkState();

                if (currentState != _state)
                {
                    if(this.Communication != null)
                        await this.Communication.UpdateNetworkState(_state);
                    if (currentState != _state && OnNetworkStatusChanged != null)
                    {
                        OnNetworkStatusChanged(this, _state);
                    }
                }
            };

            NetworkChange.NetworkAvailabilityChanged += async (s, e) =>
            {
                var currentState = _state;
                _state = GetNetworkState();

                if (currentState != _state)
                {
                    if(this.Communication != null)
                        await this.Communication.UpdateNetworkState(_state);
                    if (currentState != _state && OnNetworkStatusChanged != null)
                    {
                        OnNetworkStatusChanged(this, _state);
                    }
                }
            };
        }
    }
}