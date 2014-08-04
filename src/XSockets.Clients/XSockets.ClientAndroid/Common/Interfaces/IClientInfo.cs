using System;

namespace XSockets.ClientAndroid.Common.Interfaces
{
    public interface IClientInfo
    {
        Guid ConnectionId { get; set; }
        Guid PersistentId { get; set; }
        string Controller { get; set; }
    }
}