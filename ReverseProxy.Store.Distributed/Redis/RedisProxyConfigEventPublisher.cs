using System.Text.Json;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Store.Revisions;

namespace ReverseProxy.Store.Distributed.Redis;

public sealed partial class RedisProxyConfigEventPublisher(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<RedisProxyConfigOptions> options,
    ILogger<RedisProxyConfigEventPublisher> logger) : IProxyConfigEventPublisher
{
    private readonly RedisChannel channel = RedisChannel.Literal(options.Value.ChannelName);

    public async ValueTask PublishAsync(
        ProxyConfigPublishedEvent publishedEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publishedEvent);
        publishedEvent.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(publishedEvent);
        var subscriberCount = await connectionMultiplexer
            .GetSubscriber()
            .PublishAsync(channel, payload)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        LogPublished(logger, publishedEvent.RevisionNumber, subscriberCount);
    }

    [LoggerMessage(20, LogLevel.Debug, "Published proxy configuration revision {RevisionNumber} to {SubscriberCount} Redis subscribers.")]
    private static partial void LogPublished(ILogger logger, long revisionNumber, long subscriberCount);
}