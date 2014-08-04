using System;
using System.Runtime.Serialization;

namespace XSockets.ClientIOS.Model
{
    /// <summary>
    /// Used for storing subscription for the clients
    /// </summary>
    [Serializable]
    [DataContract]
    public class XSubscription
    {
        /// <summary>
        /// The event to subscribe to
        /// </summary>        
        [DataMember(Name = "T")]
        public string Topic { get; set; }
        /// <summary>
        /// The alias of the controller where the actionmethod is.
        /// This is set by the framework.
        /// </summary>
        [DataMember(Name = "C")]
        public string Controller { get; set; }
        /// <summary>
        /// If true the framework will send a callback when the subscription is registered in the server
        /// </summary>
        [DataMember(Name = "A")]
        public Boolean Ack { get; set; }      
  
        public XSubscription(){}
    }
}
