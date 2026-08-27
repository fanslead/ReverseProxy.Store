namespace ReverseProxy.WebApi.Contracts.V1.Runtime;

/// <summary>The proxy configuration currently applied by this runtime node.</summary>
public sealed record RuntimeStatusResponse(
    Guid RevisionId,
    long RevisionNumber,
    string ContentHash,
    DateTimeOffset PublishedAt,
    DateTimeOffset LoadedAt,
    int RouteCount,
    int ClusterCount,
    string Status);