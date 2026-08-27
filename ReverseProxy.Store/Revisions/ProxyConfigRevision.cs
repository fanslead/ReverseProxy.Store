using Yarp.ReverseProxy.Store.Configuration;

namespace Yarp.ReverseProxy.Store.Revisions;

public sealed record ProxyConfigRevision(
    Guid RevisionId,
    long RevisionNumber,
    string ContentHash,
    DateTimeOffset PublishedAt,
    ProxyConfigDocument Document);