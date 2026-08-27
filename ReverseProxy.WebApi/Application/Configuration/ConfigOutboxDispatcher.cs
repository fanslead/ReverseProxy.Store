using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReverseProxy.Store.EFCore.ControlPlane;
using ReverseProxy.WebApi.Data;
using Yarp.ReverseProxy.Store.Revisions;

namespace ReverseProxy.WebApi.Application.Configuration;

public sealed partial class ConfigOutboxDispatcher(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IProxyConfigEventPublisher eventPublisher,
    TimeProvider timeProvider,
    ILogger<ConfigOutboxDispatcher> logger)
{
    private const int MaximumErrorLength = 2000;

    public async Task<ConfigOutboxBatchResult> DispatchBatchAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        await using var dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        var messages = await dbContext.ConfigOutboxMessages
            .FromSqlInterpolated($"""
                SELECT *
                FROM config_outbox
                WHERE published_at IS NULL
                ORDER BY attempts, occurred_at, id
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var publishedCount = 0;
        var failedCount = 0;
        foreach (var message in messages)
        {
            try
            {
                var publishedEvent = Deserialize(message);
                await eventPublisher.PublishAsync(publishedEvent, cancellationToken).ConfigureAwait(false);
                message.PublishedAt = timeProvider.GetUtcNow();
                message.LastError = null;
                publishedCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.Attempts++;
                message.LastError = Truncate(exception.GetBaseException().Message, MaximumErrorLength);
                failedCount++;
                LogPublishFailed(logger, message.Id, message.RevisionNumber, message.Attempts, exception);
            }
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return new ConfigOutboxBatchResult(messages.Count, publishedCount, failedCount);
    }

    private static ProxyConfigPublishedEvent Deserialize(ConfigOutboxMessageEntity message)
    {
        if (!string.Equals(message.EventType, ProxyConfigPublishedEvent.EventType, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported outbox event type '{message.EventType}'.");
        }

        var publishedEvent = JsonSerializer.Deserialize<ProxyConfigPublishedEvent>(message.PayloadJson)
            ?? throw new InvalidDataException("The outbox event payload is empty.");
        publishedEvent.Validate();
        if (publishedEvent.RevisionId != message.RevisionId
            || publishedEvent.RevisionNumber != message.RevisionNumber)
        {
            throw new InvalidDataException("The outbox event payload does not match its envelope.");
        }

        return publishedEvent;
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    [LoggerMessage(30, LogLevel.Warning, "Failed to publish outbox message {MessageId} for revision {RevisionNumber} on attempt {Attempt}.")]
    private static partial void LogPublishFailed(
        ILogger logger,
        Guid messageId,
        long revisionNumber,
        int attempt,
        Exception exception);
}

public readonly record struct ConfigOutboxBatchResult(
    int AttemptedCount,
    int PublishedCount,
    int FailedCount);