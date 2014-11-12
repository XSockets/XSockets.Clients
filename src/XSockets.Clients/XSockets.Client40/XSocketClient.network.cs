//using System.Net.NetworkInformation;

//namespace XSockets.Client40
//{
//    public partial class XSocketClient
//    {
//        public void OnNetworkChange()
//        {
//            NetworkChange.NetworkAvailabilityChanged += (sender, eventArgs) =>
//            {
//                if (!eventArgs.IsAvailable && !this._uri.IsLoopback)
//                {
//                    //We where connected to a server on another machine... Disconnect
//                    if (this.IsConnected)
//                        this.FireOnDisconnected();
//                }
//                if (eventArgs.IsAvailable && !this.IsConnected)
//                {
//                    this.Open();
//                }
//            };
//        }
//    }
//}
