namespace ReverseProxy.Store.EFCore.Revisions;

public sealed class ProxyConfigRevisionEntity
{
    public Guid Id { get; set; }

    public long RevisionNumber { get; set; }

    public int SchemaVersion { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public string DocumentJson { get; set; } = string.Empty;

    public string PublishedBy { get; set; } = string.Empty;

    public DateTimeOffset PublishedAt { get; set; }

    public RuntimeConfigStateEntity? RuntimeState { get; set; }
}