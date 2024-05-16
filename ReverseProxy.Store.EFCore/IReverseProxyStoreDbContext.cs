namespace ReverseProxy.Store.EFCore;

public interface IReverseProxyStoreDbContext
{
    DbSet<Cluster> Clusters { get; set; }
    DbSet<ProxyRoute> ProxyRoutes { get; set; }
    DbSet<Destination> Destinations { get; set; }
    DbSet<ActiveHealthCheckOptions> ActiveHealthCheckOptions { get; set; }
    DbSet<HealthCheckOptions> HealthCheckOptions { get; set; }
    DbSet<Metadata> Metadatas { get; set; }
    DbSet<PassiveHealthCheckOptions> PassiveHealthCheckOptions { get; set; }
    DbSet<HttpClientConfig> ProxyHttpClientOptions { get; set; }
    DbSet<ProxyMatch> ProxyMatches { get; set; }
    DbSet<ForwarderRequest> RequestProxyOptions { get; set; }
    DbSet<RouteHeader> RouteHeaders { get; set; }
    DbSet<SessionAffinityConfig> SessionAffinityOptions { get; set; }
    DbSet<SessionAffinityOptionSetting> SessionAffinityOptionSettings { get; set; }
    DbSet<Transform> Transforms { get; set; }
}
