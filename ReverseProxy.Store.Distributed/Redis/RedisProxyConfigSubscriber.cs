using System.Text.Json;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Store.Revisions;

namespace ReverseProxy.Store.Distributed.Redis;

public sealed partial class RedisProxyConfigSubscriber(
    IConnectionMultiplexer connectionMultiplexer,
    RevisionedProxyConfigProvider configProvider,
    IOptions<RedisProxyConfigOptions> options,
    ILogger<RedisProxyConfigSubscriber> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = RedisChannel.Literal(options.Value.ChannelName);
        while (!stoppingToken.IsCancellationRequested)
        {
            ChannelMessageQueue? subscription = null;
            try
            {
                subscription = await connectionMultiplexer
                    .GetSubscriber()
                    .SubscribeAsync(channel)
                    .WaitAsync(stoppingToken)
                    .ConfigureAwait(false);
                subscription.OnMessage(message => HandleMessageAsync(message.Message, stoppingToken));
                LogSubscribed(logger, options.Value.ChannelName);
                await ReconcilePeriodicallyAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException exception)
            {
                LogSubscriptionFailed(logger, options.Value.ChannelName, exception);
                await Task.Delay(options.Value.SubscriptionRetryDelay, stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                if (subscription is not null)
                {
                    await subscription.UnsubscribeAsync(CommandFlags.FireAndForget).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task ReconcilePeriodicallyAsync(CancellationToken cancellationToken)
    {
        await ReconcileAsync(cancellationToken).ConfigureAwait(false);
        using var timer = new PeriodicTimer(options.Value.ReconciliationInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            await ReconcileAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await configProvider.ReloadAsync(cancellationToken).ConfigureAwait(false);
            if (result.Status == ProxyConfigApplyStatus.Applied && result.RevisionNumber is { } revisionNumber)
            {
                LogRevisionReconciled(logger, revisionNumber);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogReconciliationFailed(logger, exception);
        }
    }

    private async Task HandleMessageAsync(RedisValue payload, CancellationToken cancellationToken)
    {
        try
        {
            var publishedEvent = JsonSerializer.Deserialize<ProxyConfigPublishedEvent>(payload.ToString())
                ?? throw new InvalidDataException("The Redis configuration event payload is empty.");
            publishedEvent.Validate();

            var result = await configProvider.ReloadAsync(cancellationToken).ConfigureAwait(false);
            var appliedConfig = (RevisionedProxyConfig)configProvider.GetConfig();
            if (appliedConfig.RevisionNumber < publishedEvent.RevisionNumber
                || (appliedConfig.RevisionNumber == publishedEvent.RevisionNumber
                    && (appliedConfig.RevisionId != publishedEvent.RevisionId
                        || !string.Equals(
                            appliedConfig.ContentHash,
                            publishedEvent.ContentHash,
                            StringComparison.OrdinalIgnoreCase))))
            {
                LogRevisionMismatch(
                    logger,
                    publishedEvent.RevisionNumber,
                    publishedEvent.RevisionId,
                    appliedConfig.RevisionNumber,
                    appliedConfig.RevisionId);
                return;
            }

            LogEventHandled(logger, publishedEvent.RevisionNumber, result.Status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogEventFailed(logger, exception);
        }
    }

    [LoggerMessage(21, LogLevel.Information, "Subscribed to Redis proxy configuration channel {ChannelName}.")]
    private static partial void LogSubscribed(ILogger logger, string channelName);

    [LoggerMessage(22, LogLevel.Warning, "Failed to subscribe to Redis proxy configuration channel {ChannelName}; retrying.")]
    private static partial void LogSubscriptionFailed(ILogger logger, string channelName, Exception exception);

    [LoggerMessage(23, LogLevel.Information, "Handled proxy configuration revision {RevisionNumber} with result {ApplyStatus}.")]
    private static partial void LogEventHandled(ILogger logger, long revisionNumber, ProxyConfigApplyStatus applyStatus);

    [LoggerMessage(24, LogLevel.Warning, "Published revision {PublishedRevisionNumber} ({PublishedRevisionId}) does not match active revision {ActiveRevisionNumber} ({ActiveRevisionId}).")]
    private static partial void LogRevisionMismatch(
        ILogger logger,
        long publishedRevisionNumber,
        Guid publishedRevisionId,
        long activeRevisionNumber,
        Guid activeRevisionId);

    [LoggerMessage(25, LogLevel.Error, "Failed to handle a Redis proxy configuration event.")]
    private static partial void LogEventFailed(ILogger logger, Exception exception);

    [LoggerMessage(26, LogLevel.Information, "Reconciled proxy configuration revision {RevisionNumber} from the authoritative store.")]
    private static partial void LogRevisionReconciled(ILogger logger, long revisionNumber);

    [LoggerMessage(27, LogLevel.Warning, "Failed to reconcile proxy configuration from the authoritative store.")]
    private static partial void LogReconciliationFailed(ILogger logger, Exception exception);
}