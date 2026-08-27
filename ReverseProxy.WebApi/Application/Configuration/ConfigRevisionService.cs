using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReverseProxy.Store.EFCore.ControlPlane;
using ReverseProxy.Store.EFCore.Revisions;
using ReverseProxy.WebApi.Application.Common;
using ReverseProxy.WebApi.Contracts.V1.Common;
using ReverseProxy.WebApi.Contracts.V1.Configuration;
using ReverseProxy.WebApi.Data;
using Yarp.ReverseProxy.Store.Configuration;
using Yarp.ReverseProxy.Store.Revisions;

namespace ReverseProxy.WebApi.Application.Configuration;

public sealed class ConfigRevisionService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ConfigOperationFinalizer operationFinalizer,
    TimeProvider timeProvider) : IConfigRevisionService
{
    public async Task<PagedResponse<RevisionSummaryResponse>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var query = dbContext.ConfigRevisions.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var offset = ((long)page - 1) * pageSize;
        var revisions = offset >= total
            ? []
            : await query
                .OrderByDescending(revision => revision.RevisionNumber)
                .Skip((int)offset)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        var activeRevisionId = await dbContext.RuntimeConfigState
            .AsNoTracking()
            .Select(state => state.ActiveRevisionId)
            .SingleAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new PagedResponse<RevisionSummaryResponse>(
            revisions.Select(revision => MapSummary(revision, activeRevisionId)).ToArray(),
            page,
            pageSize,
            total);
    }

    public async Task<RevisionResponse> GetAsync(Guid revisionId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var revision = await dbContext.ConfigRevisions
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == revisionId, cancellationToken)
            ?? throw RevisionNotFound(revisionId);
        var activeRevisionId = await dbContext.RuntimeConfigState
            .AsNoTracking()
            .Select(state => state.ActiveRevisionId)
            .SingleAsync(cancellationToken);
        return Map(revision, activeRevisionId, DeserializeVerified(revision));
    }

    public async Task<ConfigOperationResponse> RollbackAsync(
        Guid revisionId,
        Guid expectedActiveRevisionId,
        RequestActor actor,
        CancellationToken cancellationToken)
    {
        if (expectedActiveRevisionId == Guid.Empty)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                "invalid_active_revision",
                "The expected active revision is invalid",
                "ExpectedActiveRevisionId must be a non-empty GUID.");
        }

        ConfigOperationEntity operation;
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var targetRevision = await dbContext.ConfigRevisions
                .AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.Id == revisionId, cancellationToken)
                ?? throw RevisionNotFound(revisionId);
            var runtimeState = await dbContext.RuntimeConfigState
                .Include(state => state.ActiveRevision)
                .SingleAsync(state => state.StateKey == RuntimeConfigStateEntity.SingletonId, cancellationToken);
            if (runtimeState.ActiveRevisionId != expectedActiveRevisionId)
            {
                throw new ApiProblemException(
                    StatusCodes.Status409Conflict,
                    "active_revision_conflict",
                    "The active configuration changed",
                    "Refresh the revision history before attempting the rollback again.");
            }

            if (targetRevision.Id == runtimeState.ActiveRevisionId)
            {
                throw new ApiProblemException(
                    StatusCodes.Status409Conflict,
                    "revision_already_active",
                    "The revision is already active",
                    $"Revision {targetRevision.RevisionNumber} is already the active configuration.");
            }

            var document = DeserializeVerified(targetRevision);
            var validation = ProxyConfigDocumentValidator.Validate(document);
            if (!validation.IsValid)
            {
                throw new ApiProblemException(
                    StatusCodes.Status422UnprocessableEntity,
                    "rollback_revision_invalid",
                    "The historical revision is no longer valid",
                    string.Join("; ", validation.Errors.Select(error => $"{error.Path}: {error.Message}")));
            }

            var now = timeProvider.GetUtcNow();
            var restoredRevision = new ProxyConfigRevisionEntity
            {
                Id = Guid.NewGuid(),
                RevisionNumber = runtimeState.ActiveRevisionNumber + 1,
                SchemaVersion = document.SchemaVersion,
                ContentHash = targetRevision.ContentHash,
                DocumentJson = ProxyConfigDocumentSerializer.SerializeCanonical(document),
                PublishedBy = actor.Id,
                PublishedAt = now
            };
            operation = new ConfigOperationEntity
            {
                Id = Guid.NewGuid(),
                Type = ConfigOperationType.Rollback,
                Status = ConfigOperationStatus.Applying,
                Revision = restoredRevision,
                RevisionId = restoredRevision.Id,
                RevisionNumber = restoredRevision.RevisionNumber,
                RequestedBy = actor.Id,
                CreatedAt = now,
                StartedAt = now
            };

            dbContext.ConfigRevisions.Add(restoredRevision);
            dbContext.ConfigOperations.Add(operation);
            dbContext.ConfigOutboxMessages.Add(new ConfigOutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                EventType = ProxyConfigPublishedEvent.EventType,
                RevisionId = restoredRevision.Id,
                RevisionNumber = restoredRevision.RevisionNumber,
                PayloadJson = JsonSerializer.Serialize(new ProxyConfigPublishedEvent(
                    restoredRevision.Id,
                    restoredRevision.RevisionNumber,
                    restoredRevision.ContentHash,
                    restoredRevision.PublishedAt)),
                OccurredAt = now
            });
            dbContext.AuditEntries.Add(new AuditEntryEntity
            {
                Id = Guid.NewGuid(),
                Action = "configuration.rolled_back",
                ResourceType = "revision",
                ResourceId = restoredRevision.Id.ToString(),
                RevisionId = restoredRevision.Id,
                ActorId = actor.Id,
                IpAddress = actor.IpAddress,
                UserAgent = actor.UserAgent,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    SourceRevisionId = targetRevision.Id,
                    SourceRevisionNumber = targetRevision.RevisionNumber,
                    PreviousRevisionId = runtimeState.ActiveRevisionId,
                    PreviousRevisionNumber = runtimeState.ActiveRevisionNumber
                }),
                OccurredAt = now
            });

            runtimeState.ActiveRevision = restoredRevision;
            runtimeState.ActiveRevisionId = restoredRevision.Id;
            runtimeState.ActiveRevisionNumber = restoredRevision.RevisionNumber;
            runtimeState.ActiveContentHash = restoredRevision.ContentHash;
            runtimeState.UpdatedAt = now;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (IsConcurrencyConflict(exception))
        {
            throw new ApiProblemException(
                StatusCodes.Status409Conflict,
                "active_revision_conflict",
                "The active configuration changed",
                "Refresh the revision history before attempting the rollback again.");
        }

        operation = await operationFinalizer.FinalizeAsync(operation.Id, CancellationToken.None);

        return Map(operation);
    }

    private static RevisionSummaryResponse MapSummary(
        ProxyConfigRevisionEntity revision,
        Guid activeRevisionId) =>
        new(
            revision.Id,
            revision.RevisionNumber,
            revision.SchemaVersion,
            revision.ContentHash,
            revision.PublishedBy,
            revision.PublishedAt,
            revision.Id == activeRevisionId);

    private static RevisionResponse Map(
        ProxyConfigRevisionEntity revision,
        Guid activeRevisionId,
        ProxyConfigDocument document) =>
        new(
            revision.Id,
            revision.RevisionNumber,
            revision.SchemaVersion,
            revision.ContentHash,
            revision.PublishedBy,
            revision.PublishedAt,
            revision.Id == activeRevisionId,
            document);

    private static ConfigOperationResponse Map(ConfigOperationEntity operation) =>
        new(
            operation.Id,
            operation.Type.ToString(),
            operation.Status.ToString(),
            operation.RevisionId,
            operation.RevisionNumber,
            operation.CreatedAt,
            operation.CompletedAt,
            operation.ErrorCode,
            operation.ErrorMessage);

    private static ApiProblemException RevisionNotFound(Guid revisionId) =>
        new(
            StatusCodes.Status404NotFound,
            "revision_not_found",
            "Revision not found",
            $"Revision '{revisionId}' does not exist.");

    private static ProxyConfigDocument DeserializeVerified(ProxyConfigRevisionEntity revision)
    {
        var document = ProxyConfigDocumentSerializer.Deserialize(revision.DocumentJson);
        var contentHash = ProxyConfigDocumentSerializer.ComputeHash(document);
        if (revision.SchemaVersion != document.SchemaVersion
            || !string.Equals(contentHash, revision.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiProblemException(
                StatusCodes.Status500InternalServerError,
                "revision_integrity_failure",
                "The historical revision failed its integrity check",
                $"Revision {revision.RevisionNumber} cannot be read because its stored metadata does not match its document.");
        }

        return document;
    }

    private static bool IsConcurrencyConflict(Exception exception) =>
        exception.GetBaseException() is PostgresException postgresException
        && postgresException.SqlState is PostgresErrorCodes.SerializationFailure
            or PostgresErrorCodes.DeadlockDetected
            or PostgresErrorCodes.UniqueViolation;
}