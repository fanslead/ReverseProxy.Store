using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReverseProxy.Store.EFCore.ControlPlane;
using ReverseProxy.WebApi.Application.Configuration;
using ReverseProxy.WebApi.Contracts.V1.Auth;
using ReverseProxy.WebApi.Contracts.V1.Common;
using ReverseProxy.WebApi.Contracts.V1.Configuration;
using ReverseProxy.WebApi.Contracts.V1.Runtime;
using ReverseProxy.WebApi.Data;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Store.Configuration;

namespace ReverseProxy.Store.Tests.WebApi;

public sealed class RevisionHistoryApiTests(PostgreSqlFixture database) :
    IClassFixture<PostgreSqlFixture>,
    IDisposable
{
    private const string AdministratorName = "revision-admin";
    private const string AdministratorPassword = "Revision.Test.2026!";

    private readonly ControlPlaneWebApplicationFactory factory = new(
        database.ConnectionString,
        AdministratorName,
        AdministratorPassword);

    private HttpClient? client;

    private HttpClient Client => client ??= factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true
    });

    [Fact]
    public async Task AdministratorCanInspectHistoryAndRestoreRevisionAsNewRevision()
    {
        await LoginAsync();
        var antiforgery = await GetAntiforgeryTokenAsync();
        var initialRuntime = Assert.IsType<RuntimeStatusResponse>(
            await Client.GetFromJsonAsync<RuntimeStatusResponse>("/api/v1/runtime/status", CancellationToken.None));

        DraftResponse draft;
        string etag;
        using (var createRequest = CreateCsrfRequest(
                   HttpMethod.Post,
                   "/api/v1/drafts",
                   antiforgery,
                   new CreateDraftRequest { Name = "Rollback integration" }))
        {
            var response = await Client.SendAsync(createRequest, CancellationToken.None);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            draft = Assert.IsType<DraftResponse>(
                await response.Content.ReadFromJsonAsync<DraftResponse>(CancellationToken.None));
            etag = Assert.IsType<string>(response.Headers.ETag?.Tag);
        }

        var firstDocument = CreateDocument("https://orders-v1.internal/");
        etag = await UpdateDraftAsync(draft, firstDocument, etag, antiforgery);
        var firstPublish = await PublishDraftAsync(draft.Id, etag, antiforgery);
        Assert.Equal("Applied", firstPublish.Status);
        Assert.Equal(1, firstPublish.RevisionNumber);

        var publishedDraftResponse = await Client.GetAsync($"/api/v1/drafts/{draft.Id}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, publishedDraftResponse.StatusCode);
        draft = Assert.IsType<DraftResponse>(
            await publishedDraftResponse.Content.ReadFromJsonAsync<DraftResponse>(CancellationToken.None));
        etag = Assert.IsType<string>(publishedDraftResponse.Headers.ETag?.Tag);

        var secondDocument = CreateDocument("https://orders-v2.internal/");
        etag = await UpdateDraftAsync(draft, secondDocument, etag, antiforgery);
        var secondPublish = await PublishDraftAsync(draft.Id, etag, antiforgery);
        Assert.Equal("Applied", secondPublish.Status);
        Assert.Equal(2, secondPublish.RevisionNumber);

        var historyBeforeRollback = Assert.IsType<PagedResponse<RevisionSummaryResponse>>(
            await Client.GetFromJsonAsync<PagedResponse<RevisionSummaryResponse>>(
                "/api/v1/revisions?page=1&pageSize=10",
                CancellationToken.None));
        Assert.Equal(3, historyBeforeRollback.Total);
        Assert.Equal([2L, 1L, 0L], historyBeforeRollback.Items.Select(item => item.RevisionNumber));
        Assert.Equal(secondPublish.RevisionId, Assert.Single(historyBeforeRollback.Items, item => item.IsActive).Id);

        var firstRevision = Assert.IsType<RevisionResponse>(
            await Client.GetFromJsonAsync<RevisionResponse>(
                $"/api/v1/revisions/{firstPublish.RevisionId}",
                CancellationToken.None));
        Assert.False(firstRevision.IsActive);
        Assert.Equal("https://orders-v1.internal/", GetDestinationAddress(firstRevision.Document));

        ConfigOperationResponse rollback;
        using (var rollbackRequest = CreateCsrfRequest(
                   HttpMethod.Post,
                   $"/api/v1/revisions/{firstRevision.Id}/rollback",
                   antiforgery,
                   new RollbackRevisionRequest { ExpectedActiveRevisionId = secondPublish.RevisionId }))
        {
            var response = await Client.SendAsync(rollbackRequest, CancellationToken.None);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            rollback = Assert.IsType<ConfigOperationResponse>(
                await response.Content.ReadFromJsonAsync<ConfigOperationResponse>(CancellationToken.None));
        }

        Assert.Equal("Rollback", rollback.Type);
        Assert.Equal("Applied", rollback.Status);
        Assert.Equal(3, rollback.RevisionNumber);
        Assert.NotEqual(firstRevision.Id, rollback.RevisionId);

        var restoredRuntime = Assert.IsType<RuntimeConfigurationResponse>(
            await Client.GetFromJsonAsync<RuntimeConfigurationResponse>(
                "/api/v1/runtime/configuration",
                CancellationToken.None));
        Assert.Equal(rollback.RevisionId, restoredRuntime.RevisionId);
        Assert.Equal(3, restoredRuntime.RevisionNumber);
        Assert.Equal(firstRevision.ContentHash, restoredRuntime.ContentHash);
        Assert.Equal("https://orders-v1.internal/", GetDestinationAddress(restoredRuntime.Document));
        var restoredRuntimeStatus = Assert.IsType<RuntimeStatusResponse>(
            await Client.GetFromJsonAsync<RuntimeStatusResponse>(
                "/api/v1/runtime/status",
                CancellationToken.None));
        Assert.Equal(rollback.RevisionId, restoredRuntimeStatus.RevisionId);
        Assert.Equal(3, restoredRuntimeStatus.RevisionNumber);
        Assert.Equal("Healthy", restoredRuntimeStatus.Status);

        var historyAfterRollback = Assert.IsType<PagedResponse<RevisionSummaryResponse>>(
            await Client.GetFromJsonAsync<PagedResponse<RevisionSummaryResponse>>(
                "/api/v1/revisions?page=1&pageSize=10",
                CancellationToken.None));
        Assert.Equal(4, historyAfterRollback.Total);
        Assert.Equal([3L, 2L, 1L, 0L], historyAfterRollback.Items.Select(item => item.RevisionNumber));
        var restoredRevision = Assert.Single(historyAfterRollback.Items, item => item.IsActive);
        Assert.Equal(rollback.RevisionId, restoredRevision.Id);
        Assert.Equal(firstRevision.ContentHash, restoredRevision.ContentHash);

        using (var staleRollbackRequest = CreateCsrfRequest(
                   HttpMethod.Post,
                   $"/api/v1/revisions/{initialRuntime.RevisionId}/rollback",
                   antiforgery,
                   new RollbackRevisionRequest { ExpectedActiveRevisionId = secondPublish.RevisionId }))
        {
            var response = await Client.SendAsync(staleRollbackRequest, CancellationToken.None);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
            Assert.Equal("active_revision_conflict", problem.GetProperty("errorCode").GetString());
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedOperation = await dbContext.ConfigOperations
            .SingleAsync(operation => operation.Id == rollback.Id, CancellationToken.None);
        Assert.Equal(ConfigOperationType.Rollback, persistedOperation.Type);
        Assert.True(await dbContext.ConfigOutboxMessages.AnyAsync(
            message => message.RevisionId == rollback.RevisionId,
            CancellationToken.None));
        Assert.True(await dbContext.AuditEntries.AnyAsync(
            audit => audit.Action == "configuration.rolled_back" && audit.RevisionId == rollback.RevisionId,
            CancellationToken.None));

        persistedOperation.Status = ConfigOperationStatus.Applying;
        persistedOperation.CompletedAt = null;
        await dbContext.SaveChangesAsync(CancellationToken.None);
        var finalizer = scope.ServiceProvider.GetRequiredService<ConfigOperationFinalizer>();
        var recoveredOperations = await Task.WhenAll(
            finalizer.FinalizeAsync(rollback.Id, CancellationToken.None),
            finalizer.FinalizeAsync(rollback.Id, CancellationToken.None));
        Assert.Contains(recoveredOperations, operation => operation.Status == ConfigOperationStatus.Applied);
        await dbContext.Entry(persistedOperation).ReloadAsync(CancellationToken.None);
        Assert.Equal(ConfigOperationStatus.Applied, persistedOperation.Status);
        Assert.NotNull(persistedOperation.CompletedAt);

        using var firstConcurrentRequest = CreateCsrfRequest(
            HttpMethod.Post,
            $"/api/v1/revisions/{firstPublish.RevisionId}/rollback",
            antiforgery,
            new RollbackRevisionRequest { ExpectedActiveRevisionId = rollback.RevisionId });
        using var secondConcurrentRequest = CreateCsrfRequest(
            HttpMethod.Post,
            $"/api/v1/revisions/{secondPublish.RevisionId}/rollback",
            antiforgery,
            new RollbackRevisionRequest { ExpectedActiveRevisionId = rollback.RevisionId });
        var concurrentResponses = await Task.WhenAll(
            Client.SendAsync(firstConcurrentRequest, CancellationToken.None),
            Client.SendAsync(secondConcurrentRequest, CancellationToken.None));
        using var firstConcurrentResponse = concurrentResponses[0];
        using var secondConcurrentResponse = concurrentResponses[1];
        Assert.Equal(
            [HttpStatusCode.Accepted, HttpStatusCode.Conflict],
            concurrentResponses.Select(response => response.StatusCode).Order().ToArray());
    }

    public void Dispose()
    {
        client?.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task LoginAsync()
    {
        var antiforgery = await GetAntiforgeryTokenAsync();
        using var request = CreateCsrfRequest(
            HttpMethod.Post,
            "/api/v1/auth/login",
            antiforgery,
            new LoginRequest
            {
                UserName = AdministratorName,
                Password = AdministratorPassword
            });
        var response = await Client.SendAsync(request, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<AntiforgeryTokenResponse> GetAntiforgeryTokenAsync() =>
        Assert.IsType<AntiforgeryTokenResponse>(
            await Client.GetFromJsonAsync<AntiforgeryTokenResponse>(
                "/api/v1/auth/antiforgery",
                CancellationToken.None));

    private async Task<string> UpdateDraftAsync(
        DraftResponse draft,
        ProxyConfigDocument document,
        string etag,
        AntiforgeryTokenResponse antiforgery)
    {
        using var request = CreateCsrfRequest(
            HttpMethod.Put,
            $"/api/v1/drafts/{draft.Id}",
            antiforgery,
            new UpdateDraftRequest { Name = draft.Name, Document = document },
            etag);
        var response = await Client.SendAsync(request, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<string>(response.Headers.ETag?.Tag);
    }

    private async Task<ConfigOperationResponse> PublishDraftAsync(
        Guid draftId,
        string etag,
        AntiforgeryTokenResponse antiforgery)
    {
        using var request = CreateCsrfRequest<object>(
            HttpMethod.Post,
            $"/api/v1/drafts/{draftId}/publish",
            antiforgery,
            etag: etag);
        var response = await Client.SendAsync(request, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return Assert.IsType<ConfigOperationResponse>(
            await response.Content.ReadFromJsonAsync<ConfigOperationResponse>(CancellationToken.None));
    }

    private static HttpRequestMessage CreateCsrfRequest<T>(
        HttpMethod method,
        string path,
        AntiforgeryTokenResponse antiforgery,
        T? body = default,
        string? etag = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(antiforgery.HeaderName, antiforgery.RequestToken);
        if (etag is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", etag);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static ProxyConfigDocument CreateDocument(string destinationAddress) =>
        new()
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
                        ["primary"] = new() { Address = destinationAddress }
                    }
                }
            ]
        };

    private static string GetDestinationAddress(ProxyConfigDocument document) =>
        Assert.Single(Assert.Single(document.Clusters).Destinations!).Value.Address;
}