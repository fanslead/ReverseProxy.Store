using Microsoft.Extensions.Options;

namespace ReverseProxy.WebApi.Application.Configuration;

public sealed partial class ConfigOperationRecoveryWorker(
    ConfigOperationFinalizer finalizer,
    IOptions<ConfigOperationRecoveryOptions> options,
    ILogger<ConfigOperationRecoveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var recoveredCount = await finalizer
                    .RecoverBatchAsync(options.Value.BatchSize, stoppingToken)
                    .ConfigureAwait(false);
                if (recoveredCount > 0)
                {
                    LogOperationsRecovered(logger, recoveredCount);
                }

                if (recoveredCount < options.Value.BatchSize)
                {
                    await Task.Delay(options.Value.PollInterval, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogRecoveryCycleFailed(logger, exception);
                await Task.Delay(options.Value.PollInterval, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    [LoggerMessage(41, LogLevel.Information, "Recovered {OperationCount} pending configuration operations.")]
    private static partial void LogOperationsRecovered(ILogger logger, int operationCount);

    [LoggerMessage(42, LogLevel.Error, "The configuration operation recovery cycle failed.")]
    private static partial void LogRecoveryCycleFailed(ILogger logger, Exception exception);
}