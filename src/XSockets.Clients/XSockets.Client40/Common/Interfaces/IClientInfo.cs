using System;

namespace XSockets.Client40.Common.Interfaces
{
    public interface IClientInfo
    {
        Guid ConnectionId { get; set; }
        Guid PersistentId { get; set; }
        string Controller { get; set; }
    }
}