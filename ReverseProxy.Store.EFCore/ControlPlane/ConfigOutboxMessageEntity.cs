namespace ReverseProxy.Store.EFCore.ControlPlane;

public sealed class ConfigOutboxMessageEntity
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public Guid RevisionId { get; set; }

    public long RevisionNumber { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}