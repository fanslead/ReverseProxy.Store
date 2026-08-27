using Microsoft.Extensions.Options;

namespace ReverseProxy.WebApi.Application.Configuration;

public sealed partial class ConfigOutboxWorker(
    ConfigOutboxDispatcher dispatcher,
    IOptions<ConfigOutboxOptions> options,
    ILogger<ConfigOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await dispatcher
                    .DispatchBatchAsync(options.Value.BatchSize, stoppingToken)
                    .ConfigureAwait(false);
                if (result.PublishedCount > 0)
                {
                    LogBatchPublished(logger, result.PublishedCount);
                }

                var delay = result.FailedCount > 0
                    ? options.Value.FailureDelay
                    : result.AttemptedCount < options.Value.BatchSize
                        ? options.Value.PollInterval
                        : TimeSpan.Zero;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogDispatchFailed(logger, exception);
                await Task.Delay(options.Value.FailureDelay, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    [LoggerMessage(31, LogLevel.Information, "Published {MessageCount} proxy configuration outbox messages.")]
    private static partial void LogBatchPublished(ILogger logger, int messageCount);

    [LoggerMessage(32, LogLevel.Error, "The proxy configuration outbox dispatch cycle failed.")]
    private static partial void LogDispatchFailed(ILogger logger, Exception exception);
}