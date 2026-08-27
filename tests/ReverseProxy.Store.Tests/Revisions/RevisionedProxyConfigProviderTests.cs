using Microsoft.Extensions.Logging.Abstractions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Store.Configuration;
using Yarp.ReverseProxy.Store.Revisions;

namespace ReverseProxy.Store.Tests.Revisions;

public sealed class RevisionedProxyConfigProviderTests
{
    [Fact]
    public void GetConfigRejectsInvalidInitialRevision()
    {
        var invalidDocument = CreateDocument("missing-cluster");
        using var provider = CreateProvider(CreateRevision(1, invalidDocument));

        Assert.Throws<InvalidDataException>(() => provider.GetConfig());
    }

    [Fact]
    public async Task ReloadAppliesNewerRevisionAndSignalsPreviousToken()
    {
        var store = new StubRevisionStore(CreateRevision(1, CreateDocument("orders")));
        using var provider = new RevisionedProxyConfigProvider(
            store,
            NullLogger<RevisionedProxyConfigProvider>.Instance);
        var initialConfig = Assert.IsType<RevisionedProxyConfig>(provider.GetConfig());

        store.ActiveRevision = CreateRevision(2, CreateDocument("orders-v2"), "orders-v2");
        var result = await provider.ReloadAsync(CancellationToken.None);

        var currentConfig = Assert.IsType<RevisionedProxyConfig>(provider.GetConfig());
        Assert.Equal(ProxyConfigApplyStatus.Applied, result.Status);
        Assert.Equal(2, currentConfig.RevisionNumber);
        Assert.True(initialConfig.ChangeToken.HasChanged);
    }

    [Fact]
    public async Task ReloadIgnoresDuplicateRevisionWithoutSignalingChange()
    {
        var revision = CreateRevision(1, CreateDocument("orders"));
        using var provider = CreateProvider(revision);
        var initialConfig = Assert.IsType<RevisionedProxyConfig>(provider.GetConfig());

        var result = await provider.ReloadAsync(CancellationToken.None);

        Assert.Equal(ProxyConfigApplyStatus.Ignored, result.Status);
        Assert.False(initialConfig.ChangeToken.HasChanged);
        Assert.Same(initialConfig, provider.GetConfig());
    }

    [Fact]
    public async Task ReloadKeepsLastKnownGoodWhenNewRevisionIsInvalid()
    {
        var store = new StubRevisionStore(CreateRevision(1, CreateDocument("orders")));
        using var provider = new RevisionedProxyConfigProvider(
            store,
            NullLogger<RevisionedProxyConfigProvider>.Instance);
        var initialConfig = Assert.IsType<RevisionedProxyConfig>(provider.GetConfig());
        store.ActiveRevision = CreateRevision(2, CreateDocument("missing-cluster"));

        var result = await provider.ReloadAsync(CancellationToken.None);

        Assert.Equal(ProxyConfigApplyStatus.Failed, result.Status);
        Assert.Equal("configuration_apply_failed", result.ErrorCode);
        Assert.False(initialConfig.ChangeToken.HasChanged);
        Assert.Same(initialConfig, provider.GetConfig());
    }

    private static RevisionedProxyConfigProvider CreateProvider(ProxyConfigRevision revision)
    {
        return new RevisionedProxyConfigProvider(
            new StubRevisionStore(revision),
            NullLogger<RevisionedProxyConfigProvider>.Instance);
    }

    private static ProxyConfigRevision CreateRevision(
        long revisionNumber,
        ProxyConfigDocument document,
        string? clusterId = null)
    {
        if (clusterId is not null)
        {
            document = CreateDocument(clusterId);
        }

        return new ProxyConfigRevision(
            Guid.NewGuid(),
            revisionNumber,
            ProxyConfigDocumentSerializer.ComputeHash(document),
            DateTimeOffset.UtcNow,
            document);
    }

    private static ProxyConfigDocument CreateDocument(string routeClusterId)
    {
        return new ProxyConfigDocument
        {
            Routes =
            [
                new RouteConfig
                {
                    RouteId = "orders-route",
                    ClusterId = routeClusterId,
                    Match = new RouteMatch { Path = "/orders/{**catch-all}" }
                }
            ],
            Clusters =
            [
                new ClusterConfig
                {
                    ClusterId = routeClusterId.StartsWith("missing", StringComparison.Ordinal) ? "orders" : routeClusterId,
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["primary"] = new() { Address = "https://orders.internal/" }
                    }
                }
            ]
        };
    }

    private sealed class StubRevisionStore(ProxyConfigRevision activeRevision) : IProxyConfigRevisionStore
    {
        public ProxyConfigRevision ActiveRevision { get; set; } = activeRevision;

        public ValueTask<ProxyConfigRevision> LoadActiveRevisionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(ActiveRevision);
        }
    }
}