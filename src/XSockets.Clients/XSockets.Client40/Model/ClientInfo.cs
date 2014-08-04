using System;
using System.Runtime.Serialization;
using XSockets.Client40.Common.Interfaces;

namespace XSockets.Client40.Model
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