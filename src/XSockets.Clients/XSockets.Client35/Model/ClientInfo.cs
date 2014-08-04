using System;
using System.Runtime.Serialization;
using XSockets.Client35.Common.Interfaces;

namespace XSockets.Client35.Model
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