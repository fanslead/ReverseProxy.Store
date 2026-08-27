using ReverseProxy.Store.EFCore.Revisions;

namespace ReverseProxy.Store.EFCore.ControlPlane;

public sealed class ProxyConfigDraftEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DocumentJson { get; set; } = string.Empty;

    public int SchemaVersion { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public Guid BaseRevisionId { get; set; }

    public long BaseRevisionNumber { get; set; }

    public long RowVersion { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public ProxyConfigRevisionEntity BaseRevision { get; set; } = null!;
}