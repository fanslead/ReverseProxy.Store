using Microsoft.EntityFrameworkCore;
using ReverseProxy.Store.EFCore.ControlPlane;
using ReverseProxy.WebApi.Data;
using Yarp.ReverseProxy.Store.Revisions;

namespace ReverseProxy.WebApi.Application.Configuration;

public sealed partial class ConfigOperationFinalizer(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    RevisionedProxyConfigProvider configProvider,
    TimeProvider timeProvider,
    ILogger<ConfigOperationFinalizer> logger)
{
    public async Task<ConfigOperationEntity> FinalizeAsync(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var operation = await dbContext.ConfigOperations
            .FromSqlInterpolated($"""
                SELECT *
                FROM config_operations
                WHERE id = {operationId}
                FOR UPDATE SKIP LOCKED
                """)
            .SingleOrDefaultAsync(cancellationToken);
        if (operation is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return await LoadAsync(operationId, cancellationToken);
        }

        if (operation.Status != ConfigOperationStatus.Applying)
        {
            await transaction.CommitAsync(cancellationToken);
            return operation;
        }

        var completion = await ApplyActiveRevisionAsync(operation, cancellationToken);
        operation.Status = completion.Status;
        operation.CompletedAt = completion.Status == ConfigOperationStatus.Applying
            ? null
            : timeProvider.GetUtcNow();
        operation.ErrorCode = completion.ErrorCode;
        operation.ErrorMessage = completion.ErrorMessage;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return operation;
    }

    public async Task<int> RecoverBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var operationIds = await dbContext.ConfigOperations
            .AsNoTracking()
            .Where(operation => operation.Status == ConfigOperationStatus.Applying)
            .OrderBy(operation => operation.CreatedAt)
            .Select(operation => operation.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
        var completedCount = 0;
        foreach (var operationId in operationIds)
        {
            var operation = await FinalizeAsync(operationId, cancellationToken);
            if (operation.Status != ConfigOperationStatus.Applying)
            {
                completedCount++;
            }
        }

        return completedCount;
    }

    private async Task<OperationCompletion> ApplyActiveRevisionAsync(
        ConfigOperationEntity operation,
        CancellationToken cancellationToken)
    {
        try
        {
            var applyResult = await configProvider.ReloadAsync(cancellationToken);
            if (applyResult.Status == ProxyConfigApplyStatus.Failed)
            {
                return new(
                    ConfigOperationStatus.Applying,
                    applyResult.ErrorCode,
                    "The revision is committed, but this node retained its last-known-good configuration. Recovery will retry.");
            }

            var appliedConfig = (RevisionedProxyConfig)configProvider.GetConfig();
            if (appliedConfig.RevisionId == operation.RevisionId
                && appliedConfig.RevisionNumber == operation.RevisionNumber)
            {
                return new(ConfigOperationStatus.Applied, null, null);
            }

            if (appliedConfig.RevisionNumber > operation.RevisionNumber)
            {
                return new(
                    ConfigOperationStatus.Partial,
                    "revision_superseded",
                    $"Revision {operation.RevisionNumber} was superseded by revision {appliedConfig.RevisionNumber} before local confirmation.");
            }

            return new(
                ConfigOperationStatus.Applying,
                "revision_not_applied",
                $"The runtime retained revision {appliedConfig.RevisionNumber} instead of applying revision {operation.RevisionNumber}. Recovery will retry.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogRecoveryFailed(logger, operation.Id, exception);
            return new(
                ConfigOperationStatus.Applying,
                "configuration_apply_failed",
                "The revision is committed, but this node could not apply the active configuration. Recovery will retry.");
        }
    }

    private async Task<ConfigOperationEntity> LoadAsync(Guid operationId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ConfigOperations
            .AsNoTracking()
            .SingleAsync(operation => operation.Id == operationId, cancellationToken);
    }

    [LoggerMessage(40, LogLevel.Error, "Failed to recover configuration operation {OperationId}.")]
    private static partial void LogRecoveryFailed(ILogger logger, Guid operationId, Exception exception);

    private sealed record OperationCompletion(
        ConfigOperationStatus Status,
        string? ErrorCode,
        string? ErrorMessage);
}