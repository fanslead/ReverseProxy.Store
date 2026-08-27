using Yarp.ReverseProxy.Store.Configuration;
using Yarp.ReverseProxy.Store.Revisions;

namespace ReverseProxy.Store.EFCore.Revisions;

public sealed class EFCoreProxyConfigRevisionStore<TContext> : IProxyConfigRevisionStore
    where TContext : DbContext, IProxyConfigRevisionDbContext
{
    private readonly IDbContextFactory<TContext> _dbContextFactory;

    public EFCoreProxyConfigRevisionStore(IDbContextFactory<TContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    public async ValueTask<ProxyConfigRevision> LoadActiveRevisionAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        var runtimeState = await dbContext.RuntimeConfigState
            .AsNoTracking()
            .Include(state => state.ActiveRevision)
            .SingleOrDefaultAsync(
                state => state.StateKey == RuntimeConfigStateEntity.SingletonId,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("The runtime configuration state has not been initialized.");

        var revision = runtimeState.ActiveRevision
            ?? throw new InvalidDataException("The active proxy configuration revision does not exist.");
        if (runtimeState.ActiveRevisionNumber != revision.RevisionNumber
            || !string.Equals(runtimeState.ActiveContentHash, revision.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The runtime configuration state does not match its active revision.");
        }

        var document = ProxyConfigDocumentSerializer.Deserialize(revision.DocumentJson);
        if (document.SchemaVersion != revision.SchemaVersion)
        {
            throw new InvalidDataException("The active proxy configuration schema version does not match its document.");
        }

        return new ProxyConfigRevision(
            revision.Id,
            revision.RevisionNumber,
            revision.ContentHash,
            revision.PublishedAt,
            document);
    }
}