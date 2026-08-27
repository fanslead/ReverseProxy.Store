using System.Text.Json.Serialization;

namespace Yarp.ReverseProxy.Store.Configuration;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ProxyConfigDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public IReadOnlyList<RouteConfig> Routes { get; init; } = [];

    public IReadOnlyList<ClusterConfig> Clusters { get; init; } = [];
}