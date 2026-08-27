namespace Yarp.ReverseProxy.Store.Revisions;

public sealed class RevisionedProxyConfig : IProxyConfig
{
    internal RevisionedProxyConfig(
        ProxyConfigRevision revision,
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters,
        IChangeToken changeToken)
    {
        RevisionId = revision.RevisionId;
        RevisionNumber = revision.RevisionNumber;
        ContentHash = revision.ContentHash;
        PublishedAt = revision.PublishedAt;
        LoadedAt = DateTimeOffset.UtcNow;
        Routes = routes;
        Clusters = clusters;
        ChangeToken = changeToken;
    }

    public Guid RevisionId { get; }

    public long RevisionNumber { get; }

    public string ContentHash { get; }

    public DateTimeOffset PublishedAt { get; }

    public DateTimeOffset LoadedAt { get; }

    public IReadOnlyList<RouteConfig> Routes { get; }

    public IReadOnlyList<ClusterConfig> Clusters { get; }

    public IChangeToken ChangeToken { get; }
}