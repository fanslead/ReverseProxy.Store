using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Store.Configuration;

namespace ReverseProxy.Store.Tests.Configuration;

public sealed class ProxyConfigDocumentTests
{
    [Fact]
    public void ComputeHashIgnoresResourceAndDictionaryOrder()
    {
        var first = CreateDocument(reverseResources: false, reverseDestinations: false, reverseTransforms: false);
        var reordered = CreateDocument(reverseResources: true, reverseDestinations: true, reverseTransforms: false);

        Assert.Equal(
            ProxyConfigDocumentSerializer.ComputeHash(first),
            ProxyConfigDocumentSerializer.ComputeHash(reordered));
    }

    [Fact]
    public void ComputeHashPreservesTransformOrder()
    {
        var first = CreateDocument(reverseResources: false, reverseDestinations: false, reverseTransforms: false);
        var reorderedTransforms = CreateDocument(reverseResources: false, reverseDestinations: false, reverseTransforms: true);

        Assert.NotEqual(
            ProxyConfigDocumentSerializer.ComputeHash(first),
            ProxyConfigDocumentSerializer.ComputeHash(reorderedTransforms));
    }

    [Fact]
    public void SerializeAndDeserializePreservesAdvancedRouteAndDestinationData()
    {
        var document = CreateDocument(reverseResources: false, reverseDestinations: false, reverseTransforms: false);

        var json = ProxyConfigDocumentSerializer.SerializeCanonical(document);
        var restored = ProxyConfigDocumentSerializer.Deserialize(json);

        var route = Assert.Single(restored.Routes, route => route.RouteId == "orders-route");
        var queryParameter = Assert.Single(route.Match.QueryParameters!);
        Assert.Equal("region", queryParameter.Name);
        Assert.Equal(QueryParameterMatchMode.Exact, queryParameter.Mode);
        Assert.Equal(2, route.Transforms!.Count);

        var cluster = Assert.Single(restored.Clusters, cluster => cluster.ClusterId == "orders");
        Assert.Equal("https://orders.internal/", cluster.Destinations!["primary"].Address);
        Assert.Equal("eu-west", cluster.Destinations["primary"].Metadata!["zone"]);
    }

    [Fact]
    public void ValidateReportsMissingClusterAndInvalidDestination()
    {
        var document = new ProxyConfigDocument
        {
            Routes =
            [
                new RouteConfig
                {
                    RouteId = "orders-route",
                    ClusterId = "missing-cluster",
                    Match = new RouteMatch { Path = "/orders/{**catch-all}" }
                }
            ],
            Clusters =
            [
                new ClusterConfig
                {
                    ClusterId = "orders",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["primary"] = new() { Address = "not-a-uri" }
                    }
                }
            ]
        };

        var result = ProxyConfigDocumentValidator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "cluster_not_found");
        Assert.Contains(result.Errors, error => error.Code == "invalid_destination_address");
    }

    [Fact]
    public void CompareReportsResourceChangesWithoutDictionaryOrderNoise()
    {
        var baseline = CreateDocument(reverseResources: false, reverseDestinations: false, reverseTransforms: false);
        var reordered = CreateDocument(reverseResources: true, reverseDestinations: true, reverseTransforms: false);
        var changed = CreateDocument(reverseResources: false, reverseDestinations: false, reverseTransforms: true);

        var reorderedDiff = ProxyConfigDocumentDiffer.Compare(baseline, reordered);
        var changedDiff = ProxyConfigDocumentDiffer.Compare(baseline, changed);

        Assert.False(reorderedDiff.HasChanges);
        Assert.True(changedDiff.HasChanges);
        Assert.Equal(["orders-route"], changedDiff.Routes.Modified);
        Assert.False(changedDiff.Clusters.HasChanges);
    }

    [Fact]
    public void ExplicitNullCollectionsNormalizeToEmptyCollections()
    {
        var document = new ProxyConfigDocument
        {
            Routes = null!,
            Clusters = null!
        };

        var validation = ProxyConfigDocumentValidator.Validate(document);
        var restored = ProxyConfigDocumentSerializer.Deserialize(
            ProxyConfigDocumentSerializer.SerializeCanonical(document));

        Assert.True(validation.IsValid);
        Assert.Empty(restored.Routes);
        Assert.Empty(restored.Clusters);
    }

    [Fact]
    public void CompareReportsDuplicateResourceIdsWithoutThrowing()
    {
        var baseline = CreateDocument(reverseResources: false, reverseDestinations: false, reverseTransforms: false);
        var duplicateRoute = new RouteConfig
        {
            RouteId = "orders-route",
            ClusterId = "orders",
            Match = new RouteMatch { Path = "/orders-v2/{**catch-all}" }
        };
        var candidate = new ProxyConfigDocument
        {
            Routes = [.. baseline.Routes, duplicateRoute],
            Clusters = baseline.Clusters
        };

        var validation = ProxyConfigDocumentValidator.Validate(candidate);
        var diff = ProxyConfigDocumentDiffer.Compare(baseline, candidate);

        Assert.Contains(validation.Errors, error => error.Code == "duplicate_route_id");
        Assert.Equal(["orders-route"], diff.Routes.Modified);
    }

    private static ProxyConfigDocument CreateDocument(
        bool reverseResources,
        bool reverseDestinations,
        bool reverseTransforms)
    {
        var transforms = new IReadOnlyDictionary<string, string>[]
        {
            new Dictionary<string, string> { ["PathRemovePrefix"] = "/orders" },
            new Dictionary<string, string> { ["RequestHeader"] = "X-Control-Plane", ["Set"] = "signal" }
        };

        if (reverseTransforms)
        {
            Array.Reverse(transforms);
        }

        var ordersRoute = new RouteConfig
        {
            RouteId = "orders-route",
            ClusterId = "orders",
            Match = new RouteMatch
            {
                Path = "/orders/{**catch-all}",
                Methods = ["GET", "POST"],
                QueryParameters =
                [
                    new RouteQueryParameter
                    {
                        Name = "region",
                        Values = ["eu"],
                        Mode = QueryParameterMatchMode.Exact
                    }
                ]
            },
            Transforms = transforms
        };

        var statusRoute = new RouteConfig
        {
            RouteId = "status-route",
            ClusterId = "status",
            Match = new RouteMatch { Path = "/status" }
        };

        var destinationEntries = new[]
        {
            new KeyValuePair<string, DestinationConfig>(
                "primary",
                new DestinationConfig
                {
                    Address = "https://orders.internal/",
                    Metadata = new Dictionary<string, string> { ["zone"] = "eu-west", ["tier"] = "primary" }
                }),
            new KeyValuePair<string, DestinationConfig>(
                "secondary",
                new DestinationConfig { Address = "https://orders-secondary.internal/" })
        };

        if (reverseDestinations)
        {
            Array.Reverse(destinationEntries);
        }

        var ordersCluster = new ClusterConfig
        {
            ClusterId = "orders",
            Destinations = destinationEntries.ToDictionary(entry => entry.Key, entry => entry.Value)
        };
        var statusCluster = new ClusterConfig
        {
            ClusterId = "status",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["default"] = new() { Address = "https://status.internal/" }
            }
        };

        return new ProxyConfigDocument
        {
            Routes = reverseResources ? [statusRoute, ordersRoute] : [ordersRoute, statusRoute],
            Clusters = reverseResources ? [statusCluster, ordersCluster] : [ordersCluster, statusCluster]
        };
    }
}