namespace ReverseProxy.Store.EFCore.ControlPlane;

public enum RuntimeNodeStatus
{
    Healthy,
    Degraded,
    Unavailable
}

public sealed class RuntimeNodeEntity
{
    public string NodeId { get; set; } = string.Empty;

    public long AppliedRevisionNumber { get; set; }

    public string AppliedContentHash { get; set; } = string.Empty;

    public RuntimeNodeStatus Status { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }
}