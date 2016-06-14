
namespace XSockets.Model
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Used for storing subscription for the clients
    /// </summary>
    public class XSubscription
    {
        /// <summary>
        /// The event to subscribe to
        /// </summary>                
        [JsonProperty("T")]
        public string Topic { get; set; }
        /// <summary>
        /// The alias of the controller where the actionmethod is.
        /// This is set by the framework.
        /// </summary>        
        [JsonProperty("C")]
        public string Controller { get; set; }
        /// <summary>
        /// If true the framework will send a callback when the subscription is registered in the server
        /// </summary>        
        [JsonProperty("A")]
        public Boolean Ack { get; set; }      
  
        public XSubscription(){}
    }
}
