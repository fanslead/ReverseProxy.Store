using ReverseProxy.Store.EFCore.Revisions;

namespace ReverseProxy.Store.EFCore.ControlPlane;

public enum ConfigOperationType
{
    Publish,
    Rollback
}

public enum ConfigOperationStatus
{
    Queued,
    Applying,
    Applied,
    Partial,
    Failed
}

public sealed class ConfigOperationEntity
{
    public Guid Id { get; set; }

    public ConfigOperationType Type { get; set; }

    public ConfigOperationStatus Status { get; set; }

    public Guid RevisionId { get; set; }

    public long RevisionNumber { get; set; }

    public string RequestedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public ProxyConfigRevisionEntity Revision { get; set; } = null!;
}