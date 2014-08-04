using System;
using System.Runtime.Serialization;
using XSockets.ClientAndroid.Common.Interfaces;

namespace XSockets.ClientAndroid.Model
{
    [DataContract]
    public class ClientInfo : IClientInfo
    {
        [DataMember(Name = "CI")]
        public Guid ConnectionId { get; set; }
        [DataMember(Name = "PI")]
        public Guid PersistentId { get; set; }
        [DataMember(Name = "C")]
        public string Controller { get; set; }
    }
}