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

public sealed class ConfigDraftService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ConfigOperationFinalizer operationFinalizer,
    TimeProvider timeProvider) : IConfigDraftService
{
    public async Task<PagedResponse<DraftSummaryResponse>> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.ConfigDrafts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(draft => EF.Functions.Like(draft.Name, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderByDescending(draft => draft.UpdatedAt)
            .ThenBy(draft => draft.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = entities.Select(MapSummary).ToArray();
        return new PagedResponse<DraftSummaryResponse>(items, page, pageSize, total);
    }

    public async Task<DraftResponse> GetAsync(Guid draftId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var draft = await dbContext.ConfigDrafts
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == draftId, cancellationToken)
            ?? throw NotFound(draftId);
        return Map(draft);
    }

    public async Task<DraftResponse> CreateAsync(
        CreateDraftRequest request,
        RequestActor actor,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedName = request.Name.Trim();
        if (await dbContext.ConfigDrafts.AnyAsync(draft => draft.Name == normalizedName, cancellationToken))
        {
            throw DuplicateName(normalizedName);
        }

        var runtimeState = await dbContext.RuntimeConfigState
            .AsNoTracking()
            .Include(state => state.ActiveRevision)
            .SingleAsync(state => state.StateKey == RuntimeConfigStateEntity.SingletonId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var draft = new ProxyConfigDraftEntity
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            DocumentJson = runtimeState.ActiveRevision.DocumentJson,
            SchemaVersion = runtimeState.ActiveRevision.SchemaVersion,
            ContentHash = runtimeState.ActiveRevision.ContentHash,
            BaseRevisionId = runtimeState.ActiveRevisionId,
            BaseRevisionNumber = runtimeState.ActiveRevisionNumber,
            RowVersion = 1,
            CreatedBy = actor.Id,
            CreatedAt = now,
            UpdatedBy = actor.Id,
            UpdatedAt = now
        };
        dbContext.ConfigDrafts.Add(draft);
        dbContext.AuditEntries.Add(CreateAudit("draft.created", "draft", draft.Id.ToString(), null, actor, now, "{}"));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new ApiProblemException(
                StatusCodes.Status409Conflict,
                "draft_name_conflict",
                "A draft with this name already exists",
                exception.InnerException is null ? "Choose a unique draft name." : "Choose a unique draft name.");
        }

        return Map(draft);
    }

    public async Task<DraftResponse> UpdateAsync(
        Guid draftId,
        UpdateDraftRequest request,
        long expectedRowVersion,
        RequestActor actor,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var draft = await dbContext.ConfigDrafts.SingleOrDefaultAsync(entity => entity.Id == draftId, cancellationToken)
            ?? throw NotFound(draftId);
        EnsureVersion(draft, expectedRowVersion);

        var normalizedName = request.Name.Trim();
        if (await dbContext.ConfigDrafts.AnyAsync(
                entity => entity.Id != draftId && entity.Name == normalizedName,
                cancellationToken))
        {
            throw DuplicateName(normalizedName);
        }

        var canonicalJson = ProxyConfigDocumentSerializer.SerializeCanonical(request.Document);
        draft.Name = normalizedName;
        draft.DocumentJson = canonicalJson;
        draft.SchemaVersion = request.Document.SchemaVersion;
        draft.ContentHash = ProxyConfigDocumentSerializer.ComputeHash(request.Document);
        draft.RowVersion++;
        draft.UpdatedBy = actor.Id;
        draft.UpdatedAt = timeProvider.GetUtcNow();
        dbContext.AuditEntries.Add(CreateAudit(
            "draft.updated",
            "draft",
            draft.Id.ToString(),
            null,
            actor,
            draft.UpdatedAt,
            JsonSerializer.Serialize(new { draft.RowVersion, draft.ContentHash })));

        await SaveWithConcurrencyCheckAsync(dbContext, draftId, cancellationToken);
        return Map(draft);
    }

    public async Task DeleteAsync(
        Guid draftId,
        long expectedRowVersion,
        RequestActor actor,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var draft = await dbContext.ConfigDrafts.SingleOrDefaultAsync(entity => entity.Id == draftId, cancellationToken)
            ?? throw NotFound(draftId);
        EnsureVersion(draft, expectedRowVersion);
        dbContext.ConfigDrafts.Remove(draft);
        dbContext.AuditEntries.Add(CreateAudit(
            "draft.deleted",
            "draft",
            draft.Id.ToString(),
            null,
            actor,
            timeProvider.GetUtcNow(),
            "{}"));
        await SaveWithConcurrencyCheckAsync(dbContext, draftId, cancellationToken);
    }

    public async Task<ConfigValidationResponse> ValidateAsync(Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await GetAsync(draftId, cancellationToken);
        var result = ProxyConfigDocumentValidator.Validate(draft.Document);
        return new ConfigValidationResponse(
            result.IsValid,
            draft.ContentHash,
            result.Errors.Select(error => new ConfigValidationErrorResponse(error.Code, error.Path, error.Message)).ToArray());
    }

    public async Task<ConfigDiffResponse> DiffAsync(Guid draftId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var draft = await dbContext.ConfigDrafts
            .AsNoTracking()
            .Include(entity => entity.BaseRevision)
            .SingleOrDefaultAsync(entity => entity.Id == draftId, cancellationToken)
            ?? throw NotFound(draftId);
        var baseline = ProxyConfigDocumentSerializer.Deserialize(draft.BaseRevision.DocumentJson);
        var candidate = ProxyConfigDocumentSerializer.Deserialize(draft.DocumentJson);
        return Map(ProxyConfigDocumentDiffer.Compare(baseline, candidate));
    }

    public async Task<ConfigOperationResponse> PublishAsync(
        Guid draftId,
        long expectedRowVersion,
        RequestActor actor,
        CancellationToken cancellationToken)
    {
        ConfigOperationEntity operation;
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var draft = await dbContext.ConfigDrafts
                .Include(entity => entity.BaseRevision)
                .SingleOrDefaultAsync(entity => entity.Id == draftId, cancellationToken)
                ?? throw NotFound(draftId);
            EnsureVersion(draft, expectedRowVersion);

            var document = ProxyConfigDocumentSerializer.Deserialize(draft.DocumentJson);
            var validation = ProxyConfigDocumentValidator.Validate(document);
            if (!validation.IsValid)
            {
                throw new ApiProblemException(
                    StatusCodes.Status422UnprocessableEntity,
                    "configuration_invalid",
                    "The draft configuration is invalid",
                    string.Join("; ", validation.Errors.Select(error => $"{error.Path}: {error.Message}")));
            }

            var canonicalJson = ProxyConfigDocumentSerializer.SerializeCanonical(document);
            var contentHash = ProxyConfigDocumentSerializer.ComputeHash(document);
            if (draft.SchemaVersion != document.SchemaVersion
                || !string.Equals(draft.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new ApiProblemException(
                    StatusCodes.Status500InternalServerError,
                    "draft_integrity_failure",
                    "The draft failed its integrity check",
                    $"Draft '{draft.Id}' cannot be published because its stored metadata does not match its document.");
            }

            var runtimeState = await dbContext.RuntimeConfigState
                .Include(state => state.ActiveRevision)
                .SingleAsync(state => state.StateKey == RuntimeConfigStateEntity.SingletonId, cancellationToken);
            if (draft.BaseRevisionId != runtimeState.ActiveRevisionId)
            {
                throw new ApiProblemException(
                    StatusCodes.Status409Conflict,
                    "draft_base_revision_conflict",
                    "The active configuration changed",
                    "Create a new draft from the current revision before publishing.");
            }

            var now = timeProvider.GetUtcNow();
            var revision = new ProxyConfigRevisionEntity
            {
                Id = Guid.NewGuid(),
                RevisionNumber = runtimeState.ActiveRevisionNumber + 1,
                SchemaVersion = document.SchemaVersion,
                ContentHash = contentHash,
                DocumentJson = canonicalJson,
                PublishedBy = actor.Id,
                PublishedAt = now
            };
            operation = new ConfigOperationEntity
            {
                Id = Guid.NewGuid(),
                Type = ConfigOperationType.Publish,
                Status = ConfigOperationStatus.Applying,
                Revision = revision,
                RevisionId = revision.Id,
                RevisionNumber = revision.RevisionNumber,
                RequestedBy = actor.Id,
                CreatedAt = now,
                StartedAt = now
            };
            var diff = ProxyConfigDocumentDiffer.Compare(
                ProxyConfigDocumentSerializer.Deserialize(runtimeState.ActiveRevision.DocumentJson),
                document);

            dbContext.ConfigRevisions.Add(revision);
            dbContext.ConfigOperations.Add(operation);
            dbContext.ConfigOutboxMessages.Add(new ConfigOutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                EventType = ProxyConfigPublishedEvent.EventType,
                RevisionId = revision.Id,
                RevisionNumber = revision.RevisionNumber,
                PayloadJson = JsonSerializer.Serialize(new ProxyConfigPublishedEvent(
                    revision.Id,
                    revision.RevisionNumber,
                    revision.ContentHash,
                    revision.PublishedAt)),
                OccurredAt = now
            });
            dbContext.AuditEntries.Add(CreateAudit(
                "configuration.published",
                "revision",
                revision.Id.ToString(),
                revision.Id,
                actor,
                now,
                JsonSerializer.Serialize(diff)));

            runtimeState.ActiveRevision = revision;
            runtimeState.ActiveRevisionId = revision.Id;
            runtimeState.ActiveRevisionNumber = revision.RevisionNumber;
            runtimeState.ActiveContentHash = revision.ContentHash;
            runtimeState.UpdatedAt = now;
            draft.BaseRevision = revision;
            draft.BaseRevisionId = revision.Id;
            draft.BaseRevisionNumber = revision.RevisionNumber;
            draft.RowVersion++;
            draft.UpdatedBy = actor.Id;
            draft.UpdatedAt = now;

            await SaveWithConcurrencyCheckAsync(dbContext, draftId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (IsConcurrencyConflict(exception))
        {
            throw new ApiProblemException(
                StatusCodes.Status409Conflict,
                "active_revision_conflict",
                "The active configuration changed",
                "Refresh the draft before attempting to publish again.");
        }

        operation = await operationFinalizer.FinalizeAsync(operation.Id, CancellationToken.None);

        return Map(operation);
    }

    public async Task<ConfigOperationResponse> GetOperationAsync(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var operation = await dbContext.ConfigOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == operationId, cancellationToken)
            ?? throw new ApiProblemException(
                StatusCodes.Status404NotFound,
                "operation_not_found",
                "Operation not found",
                $"Operation '{operationId}' does not exist.");
        return Map(operation);
    }

    private static DraftSummaryResponse MapSummary(ProxyConfigDraftEntity draft)
    {
        var document = ProxyConfigDocumentSerializer.Deserialize(draft.DocumentJson);
        return new DraftSummaryResponse(
            draft.Id,
            draft.Name,
            draft.BaseRevisionNumber,
            draft.RowVersion,
            draft.ContentHash,
            document.Routes.Count,
            document.Clusters.Count,
            draft.UpdatedBy,
            draft.UpdatedAt);
    }

    private static DraftResponse Map(ProxyConfigDraftEntity draft)
    {
        return new DraftResponse(
            draft.Id,
            draft.Name,
            draft.BaseRevisionNumber,
            draft.RowVersion,
            draft.ContentHash,
            draft.UpdatedBy,
            draft.UpdatedAt,
            ProxyConfigDocumentSerializer.Deserialize(draft.DocumentJson));
    }

    private static ConfigDiffResponse Map(ProxyConfigDocumentDiff diff)
    {
        return new ConfigDiffResponse(
            diff.BaseHash,
            diff.CandidateHash,
            diff.HasChanges,
            new ConfigResourceDiffResponse(diff.Routes.Added, diff.Routes.Removed, diff.Routes.Modified),
            new ConfigResourceDiffResponse(diff.Clusters.Added, diff.Clusters.Removed, diff.Clusters.Modified));
    }

    private static ConfigOperationResponse Map(ConfigOperationEntity operation)
    {
        return new ConfigOperationResponse(
            operation.Id,
            operation.Type.ToString(),
            operation.Status.ToString(),
            operation.RevisionId,
            operation.RevisionNumber,
            operation.CreatedAt,
            operation.CompletedAt,
            operation.ErrorCode,
            operation.ErrorMessage);
    }

    private static AuditEntryEntity CreateAudit(
        string action,
        string resourceType,
        string? resourceId,
        Guid? revisionId,
        RequestActor actor,
        DateTimeOffset occurredAt,
        string detailsJson)
    {
        return new AuditEntryEntity
        {
            Id = Guid.NewGuid(),
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            RevisionId = revisionId,
            ActorId = actor.Id,
            IpAddress = actor.IpAddress,
            UserAgent = actor.UserAgent,
            DetailsJson = detailsJson,
            OccurredAt = occurredAt
        };
    }

    private static async Task SaveWithConcurrencyCheckAsync(
        ApplicationDbContext dbContext,
        Guid draftId,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw StaleVersion(draftId);
        }
    }

    private static void EnsureVersion(ProxyConfigDraftEntity draft, long expectedRowVersion)
    {
        if (draft.RowVersion != expectedRowVersion)
        {
            throw StaleVersion(draft.Id);
        }
    }

    private static ApiProblemException NotFound(Guid draftId) => new(
        StatusCodes.Status404NotFound,
        "draft_not_found",
        "Draft not found",
        $"Draft '{draftId}' does not exist.");

    private static ApiProblemException StaleVersion(Guid draftId) => new(
        StatusCodes.Status412PreconditionFailed,
        "draft_version_conflict",
        "The draft has changed",
        $"Draft '{draftId}' was modified by another request. Reload it before saving.");

    private static ApiProblemException DuplicateName(string name) => new(
        StatusCodes.Status409Conflict,
        "draft_name_conflict",
        "A draft with this name already exists",
        $"Draft name '{name}' is already in use.");

    private static bool IsConcurrencyConflict(Exception exception) =>
        exception.GetBaseException() is PostgresException postgresException
        && postgresException.SqlState is PostgresErrorCodes.SerializationFailure
            or PostgresErrorCodes.DeadlockDetected
            or PostgresErrorCodes.UniqueViolation;
}