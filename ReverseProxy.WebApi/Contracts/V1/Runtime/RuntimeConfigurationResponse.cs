using Yarp.ReverseProxy.Store.Configuration;

namespace ReverseProxy.WebApi.Contracts.V1.Runtime;

/// <summary>The canonical proxy configuration currently active in the control plane.</summary>
public sealed record RuntimeConfigurationResponse(
    Guid RevisionId,
    long RevisionNumber,
    string ContentHash,
    DateTimeOffset PublishedAt,
    ProxyConfigDocument Document);