using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReverseProxy.Store.EFCore.ControlPlane;
using ReverseProxy.Store.EFCore.Revisions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Store.Configuration;

namespace ReverseProxy.Store.Tests.EFCore;

public sealed class EFCoreProxyConfigRevisionStoreTests
{
    [Fact]
    public async Task LoadActiveRevisionReturnsMatchingDocument()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        var document = CreateDocument();
        var revision = CreateRevision(document);

        await using (var dbContext = new TestDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();
            dbContext.ConfigRevisions.Add(revision);
            dbContext.RuntimeConfigState.Add(new RuntimeConfigStateEntity
            {
                ActiveRevision = revision,
                ActiveRevisionId = revision.Id,
                ActiveRevisionNumber = revision.RevisionNumber,
                ActiveContentHash = revision.ContentHash,
                UpdatedAt = revision.PublishedAt
            });
            await dbContext.SaveChangesAsync();
        }

        var store = new EFCoreProxyConfigRevisionStore<TestDbContext>(new TestDbContextFactory(options));
        var loaded = await store.LoadActiveRevisionAsync(CancellationToken.None);

        Assert.Equal(revision.Id, loaded.RevisionId);
        Assert.Equal(revision.RevisionNumber, loaded.RevisionNumber);
        Assert.Equal(revision.ContentHash, loaded.ContentHash);
        Assert.Equal("orders-route", Assert.Single(loaded.Document.Routes).RouteId);
    }

    [Fact]
    public async Task LoadActiveRevisionRejectsMismatchedRuntimePointer()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        var revision = CreateRevision(CreateDocument());

        await using (var dbContext = new TestDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();
            dbContext.ConfigRevisions.Add(revision);
            dbContext.RuntimeConfigState.Add(new RuntimeConfigStateEntity
            {
                ActiveRevision = revision,
                ActiveRevisionId = revision.Id,
                ActiveRevisionNumber = revision.RevisionNumber + 1,
                ActiveContentHash = revision.ContentHash,
                UpdatedAt = revision.PublishedAt
            });
            await dbContext.SaveChangesAsync();
        }

        var store = new EFCoreProxyConfigRevisionStore<TestDbContext>(new TestDbContextFactory(options));

        await Assert.ThrowsAsync<InvalidDataException>(
            async () => await store.LoadActiveRevisionAsync(CancellationToken.None));
    }

    [Fact]
    public void ControlPlaneModelMarksDraftVersionAsConcurrencyToken()
    {
        var options = new DbContextOptionsBuilder<ControlPlaneTestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var dbContext = new ControlPlaneTestDbContext(options);

        var draftEntity = dbContext.Model.FindEntityType(typeof(ProxyConfigDraftEntity));
        var rowVersion = draftEntity?.FindProperty(nameof(ProxyConfigDraftEntity.RowVersion));

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.True(draftEntity!.GetIndexes().Single(index =>
            index.Properties.Single().Name == nameof(ProxyConfigDraftEntity.Name)).IsUnique);
    }

    private static ProxyConfigRevisionEntity CreateRevision(ProxyConfigDocument document)
    {
        return new ProxyConfigRevisionEntity
        {
            Id = Guid.NewGuid(),
            RevisionNumber = 1,
            SchemaVersion = document.SchemaVersion,
            ContentHash = ProxyConfigDocumentSerializer.ComputeHash(document),
            DocumentJson = ProxyConfigDocumentSerializer.SerializeCanonical(document),
            PublishedBy = "test",
            PublishedAt = DateTimeOffset.UtcNow
        };
    }

    private static ProxyConfigDocument CreateDocument()
    {
        return new ProxyConfigDocument
        {
            Routes =
            [
                new RouteConfig
                {
                    RouteId = "orders-route",
                    ClusterId = "orders",
                    Match = new RouteMatch { Path = "/orders/{**catch-all}" }
                }
            ],
            Clusters =
            [
                new ClusterConfig
                {
                    ClusterId = "orders",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["primary"] = new() { Address = "https://orders.internal/" }
                    }
                }
            ]
        };
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : DbContext(options), IProxyConfigRevisionDbContext
    {
        public DbSet<ProxyConfigRevisionEntity> ConfigRevisions => Set<ProxyConfigRevisionEntity>();

        public DbSet<RuntimeConfigStateEntity> RuntimeConfigState => Set<RuntimeConfigStateEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureProxyConfigRevisionStore();
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<TestDbContext> options)
        : IDbContextFactory<TestDbContext>
    {
        public TestDbContext CreateDbContext() => new(options);
    }

    private sealed class ControlPlaneTestDbContext(DbContextOptions<ControlPlaneTestDbContext> options)
        : DbContext(options), IProxyConfigControlPlaneDbContext
    {
        public DbSet<ProxyConfigRevisionEntity> ConfigRevisions => Set<ProxyConfigRevisionEntity>();

        public DbSet<RuntimeConfigStateEntity> RuntimeConfigState => Set<RuntimeConfigStateEntity>();

        public DbSet<ProxyConfigDraftEntity> ConfigDrafts => Set<ProxyConfigDraftEntity>();

        public DbSet<ConfigOperationEntity> ConfigOperations => Set<ConfigOperationEntity>();

        public DbSet<ConfigOutboxMessageEntity> ConfigOutboxMessages => Set<ConfigOutboxMessageEntity>();

        public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();

        public DbSet<RuntimeNodeEntity> RuntimeNodes => Set<RuntimeNodeEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureProxyConfigControlPlane();
        }
    }
}