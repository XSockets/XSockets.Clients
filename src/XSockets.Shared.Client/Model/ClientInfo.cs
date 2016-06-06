
namespace XSockets.Model
{
    using System;
    using Newtonsoft.Json;
    using XSockets.Common.Interfaces;

    public class ClientInfo : IClientInfo
    {
        [JsonProperty("CI")]
        public Guid ConnectionId { get; set; }
        [JsonProperty("PI")]
        public Guid PersistentId { get; set; }
        [JsonProperty("C")]
        public string Controller { get; set; }
    }
}