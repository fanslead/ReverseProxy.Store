namespace ReverseProxy.Store.EFCore.ControlPlane;

public sealed class AuditEntryEntity
{
    public Guid Id { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string? ResourceId { get; set; }

    public Guid? RevisionId { get; set; }

    public string ActorId { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string DetailsJson { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }
}