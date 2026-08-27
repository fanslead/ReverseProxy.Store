namespace ReverseProxy.Store.EFCore.Revisions;

public sealed class RuntimeConfigStateEntity
{
    public const int SingletonId = 1;

    public int StateKey { get; set; } = SingletonId;

    public Guid ActiveRevisionId { get; set; }

    public long ActiveRevisionNumber { get; set; }

    public string ActiveContentHash { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public ProxyConfigRevisionEntity ActiveRevision { get; set; } = null!;
}