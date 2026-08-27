using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ReverseProxy.Store.EFCore.ControlPlane;
using ReverseProxy.WebApi.Application.Configuration;
using ReverseProxy.WebApi.Data;
using Yarp.ReverseProxy.Store.Revisions;

namespace ReverseProxy.Store.Tests.WebApi;

public sealed class ConfigOutboxDispatcherTests(PostgreSqlFixture database) :
    IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task DispatchBatchPublishesAndAcknowledgesMessage()
    {
        var dbContextFactory = await CreateDbContextFactoryAsync();
        var publishedEvent = CreatePublishedEvent(1);
        await AddMessageAsync(dbContextFactory, publishedEvent);
        var publisher = new RecordingPublisher();
        var dispatcher = CreateDispatcher(dbContextFactory, publisher);

        var result = await dispatcher.DispatchBatchAsync(32, CancellationToken.None);

        Assert.Equal(new ConfigOutboxBatchResult(1, 1, 0), result);
        Assert.Equal(publishedEvent, Assert.Single(publisher.Events));
        await using var dbContext = dbContextFactory.CreateDbContext();
        var message = await dbContext.ConfigOutboxMessages.AsNoTracking().SingleAsync();
        Assert.NotNull(message.PublishedAt);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.LastError);
    }

    [Fact]
    public async Task DispatchBatchRecordsFailureAndRetriesMessage()
    {
        var dbContextFactory = await CreateDbContextFactoryAsync();
        var publishedEvent = CreatePublishedEvent(2);
        await AddMessageAsync(dbContextFactory, publishedEvent);
        var publisher = new RecordingPublisher(new InvalidOperationException("Redis is unavailable."));
        var dispatcher = CreateDispatcher(dbContextFactory, publisher);

        var failedResult = await dispatcher.DispatchBatchAsync(32, CancellationToken.None);

        Assert.Equal(new ConfigOutboxBatchResult(1, 0, 1), failedResult);
        await using (var dbContext = dbContextFactory.CreateDbContext())
        {
            var failedMessage = await dbContext.ConfigOutboxMessages.AsNoTracking().SingleAsync();
            Assert.Null(failedMessage.PublishedAt);
            Assert.Equal(1, failedMessage.Attempts);
            Assert.Equal("Redis is unavailable.", failedMessage.LastError);
        }

        var successfulResult = await dispatcher.DispatchBatchAsync(32, CancellationToken.None);

        Assert.Equal(new ConfigOutboxBatchResult(1, 1, 0), successfulResult);
        Assert.Equal(publishedEvent, Assert.Single(publisher.Events));
        await using (var dbContext = dbContextFactory.CreateDbContext())
        {
            var publishedMessage = await dbContext.ConfigOutboxMessages.AsNoTracking().SingleAsync();
            Assert.NotNull(publishedMessage.PublishedAt);
            Assert.Equal(1, publishedMessage.Attempts);
            Assert.Null(publishedMessage.LastError);
        }
    }

    [Fact]
    public async Task ConcurrentDispatcherSkipsMessageLockedByAnotherInstance()
    {
        var dbContextFactory = await CreateDbContextFactoryAsync();
        await AddMessageAsync(dbContextFactory, CreatePublishedEvent(3));
        var publisher = new BlockingPublisher();
        var firstDispatcher = CreateDispatcher(dbContextFactory, publisher);
        var secondDispatcher = CreateDispatcher(dbContextFactory, publisher);

        var firstDispatch = firstDispatcher.DispatchBatchAsync(1, CancellationToken.None);
        await publisher.FirstPublishStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var secondResult = await secondDispatcher.DispatchBatchAsync(1, CancellationToken.None);
        publisher.ReleaseFirstPublish.TrySetResult();
        var firstResult = await firstDispatch;

        Assert.Equal(new ConfigOutboxBatchResult(1, 1, 0), firstResult);
        Assert.Equal(new ConfigOutboxBatchResult(0, 0, 0), secondResult);
        Assert.Equal(1, publisher.PublishCount);
    }

    private async Task<IDbContextFactory<ApplicationDbContext>> CreateDbContextFactoryAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options;
        var factory = new TestDbContextFactory(options);
        await using var dbContext = factory.CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.ConfigOutboxMessages.ExecuteDeleteAsync();
        return factory;
    }

    private static async Task AddMessageAsync(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ProxyConfigPublishedEvent publishedEvent)
    {
        await using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.ConfigOutboxMessages.Add(new ConfigOutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            EventType = ProxyConfigPublishedEvent.EventType,
            RevisionId = publishedEvent.RevisionId,
            RevisionNumber = publishedEvent.RevisionNumber,
            PayloadJson = JsonSerializer.Serialize(publishedEvent),
            OccurredAt = publishedEvent.PublishedAt
        });
        await dbContext.SaveChangesAsync();
    }

    private static ProxyConfigPublishedEvent CreatePublishedEvent(long revisionNumber) =>
        new(Guid.NewGuid(), revisionNumber, new string('a', 64), DateTimeOffset.UtcNow);

    private static ConfigOutboxDispatcher CreateDispatcher(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IProxyConfigEventPublisher publisher) =>
        new(dbContextFactory, publisher, TimeProvider.System, NullLogger<ConfigOutboxDispatcher>.Instance);

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    private sealed class RecordingPublisher(params Exception[] failures) : IProxyConfigEventPublisher
    {
        private readonly Queue<Exception> failures = new(failures);

        public List<ProxyConfigPublishedEvent> Events { get; } = [];

        public ValueTask PublishAsync(
            ProxyConfigPublishedEvent publishedEvent,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (failures.TryDequeue(out var failure))
            {
                throw failure;
            }

            Events.Add(publishedEvent);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BlockingPublisher : IProxyConfigEventPublisher
    {
        private int publishCount;

        public TaskCompletionSource FirstPublishStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirstPublish { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int PublishCount => publishCount;

        public async ValueTask PublishAsync(
            ProxyConfigPublishedEvent publishedEvent,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref publishCount) != 1)
            {
                return;
            }

            FirstPublishStarted.TrySetResult();
            await ReleaseFirstPublish.Task.WaitAsync(cancellationToken);
        }
    }
}