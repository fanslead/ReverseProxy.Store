namespace ReverseProxy.Store.Distributed.Redis;

public sealed class RedisProxyConfigOptions
{
    public const string DefaultChannelName = "reverseproxy:config:published";

    public string ChannelName { get; set; } = DefaultChannelName;

    public TimeSpan SubscriptionRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan ReconciliationInterval { get; set; } = TimeSpan.FromSeconds(30);
}