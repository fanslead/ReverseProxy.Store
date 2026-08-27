using ReverseProxy.Store.Distributed.Redis;
using Yarp.ReverseProxy.Store.Revisions;

namespace Microsoft.Extensions.DependencyInjection;

public static class RedisServiceCollectionExtension
{
    public static IReverseProxyBuilder AddRedisRevisionDistribution(
        this IReverseProxyBuilder builder,
        string connectionString,
        string? channelName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.Services.AddOptions<RedisProxyConfigOptions>()
            .Configure(options =>
            {
                if (!string.IsNullOrWhiteSpace(channelName))
                {
                    options.ChannelName = channelName;
                }
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.ChannelName), "The Redis channel name is required.")
            .Validate(options => options.SubscriptionRetryDelay > TimeSpan.Zero, "The Redis subscription retry delay must be positive.")
            .Validate(options => options.ReconciliationInterval > TimeSpan.Zero, "The Redis reconciliation interval must be positive.")
            .ValidateOnStart();
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var configuration = ConfigurationOptions.Parse(connectionString);
            configuration.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(configuration);
        });
        builder.Services.AddSingleton<IProxyConfigEventPublisher, RedisProxyConfigEventPublisher>();
        builder.Services.AddHostedService<RedisProxyConfigSubscriber>();
        return builder;
    }
}
