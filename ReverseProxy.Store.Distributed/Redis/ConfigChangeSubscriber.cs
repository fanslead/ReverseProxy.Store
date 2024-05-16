namespace ReverseProxy.Store.Distributed.Redis;

public class ConfigChangeSubscriber : BackgroundService
{
    private readonly IReverseProxyStore _reverseProxyStore;

    public ConfigChangeSubscriber(IReverseProxyStore reverseProxyStore, IConnectionMultiplexer connectionMultiplexer)
    {
        _reverseProxyStore = reverseProxyStore;
        var sub = connectionMultiplexer.GetSubscriber();
        sub.Subscribe(RedisChannel.Literal("ConfigChange") , (channel, value) => _reverseProxyStore.ReloadConfig());
        _reverseProxyStore.ChangeConfig -= _reverseProxyStore.ReloadConfig;
        _reverseProxyStore.ChangeConfig += () =>
            sub.Publish(RedisChannel.Literal("ConfigChange"), "")
        ;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
