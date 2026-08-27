namespace Yarp.ReverseProxy.Store.Configuration;

public sealed record ProxyConfigValidationError(string Code, string Path, string Message);

public sealed record ProxyConfigValidationResult(IReadOnlyList<ProxyConfigValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static class ProxyConfigDocumentValidator
{
    public static ProxyConfigValidationResult Validate(ProxyConfigDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var errors = new List<ProxyConfigValidationError>();
        if (document.SchemaVersion != ProxyConfigDocument.CurrentSchemaVersion)
        {
            errors.Add(new(
                "unsupported_schema_version",
                "schemaVersion",
                $"Schema version {document.SchemaVersion} is not supported."));
        }

        var clusterIds = new HashSet<string>(StringComparer.Ordinal);
        var clusters = document.Clusters ?? [];
        for (var index = 0; index < clusters.Count; index++)
        {
            var cluster = clusters[index];
            var clusterPath = $"clusters[{index}]";
            if (string.IsNullOrWhiteSpace(cluster.ClusterId))
            {
                errors.Add(new("required", $"{clusterPath}.clusterId", "Cluster ID is required."));
                continue;
            }

            if (!clusterIds.Add(cluster.ClusterId))
            {
                errors.Add(new("duplicate_cluster_id", $"{clusterPath}.clusterId", $"Cluster ID '{cluster.ClusterId}' is duplicated."));
            }

            ValidateDestinations(cluster, clusterPath, errors);
        }

        var routeIds = new HashSet<string>(StringComparer.Ordinal);
        var routes = document.Routes ?? [];
        for (var index = 0; index < routes.Count; index++)
        {
            var route = routes[index];
            var routePath = $"routes[{index}]";
            if (string.IsNullOrWhiteSpace(route.RouteId))
            {
                errors.Add(new("required", $"{routePath}.routeId", "Route ID is required."));
            }
            else if (!routeIds.Add(route.RouteId))
            {
                errors.Add(new("duplicate_route_id", $"{routePath}.routeId", $"Route ID '{route.RouteId}' is duplicated."));
            }

            if (!string.IsNullOrWhiteSpace(route.ClusterId) && !clusterIds.Contains(route.ClusterId))
            {
                errors.Add(new(
                    "cluster_not_found",
                    $"{routePath}.clusterId",
                    $"Cluster '{route.ClusterId}' does not exist in this document."));
            }
        }

        return new ProxyConfigValidationResult(errors);
    }

    private static void ValidateDestinations(
        ClusterConfig cluster,
        string clusterPath,
        List<ProxyConfigValidationError> errors)
    {
        if (cluster.Destinations is null)
        {
            return;
        }

        foreach (var destination in cluster.Destinations)
        {
            var destinationPath = $"{clusterPath}.destinations.{destination.Key}";
            if (string.IsNullOrWhiteSpace(destination.Key))
            {
                errors.Add(new("required", $"{clusterPath}.destinations", "Destination name is required."));
            }

            if (string.IsNullOrWhiteSpace(destination.Value.Address))
            {
                errors.Add(new("required", $"{destinationPath}.address", "Destination address is required."));
                continue;
            }

            if (!Uri.TryCreate(destination.Value.Address, UriKind.Absolute, out var address)
                || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add(new(
                    "invalid_destination_address",
                    $"{destinationPath}.address",
                    "Destination address must be an absolute HTTP or HTTPS URI."));
            }
        }
    }
}