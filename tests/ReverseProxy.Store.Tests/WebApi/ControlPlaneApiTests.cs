using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReverseProxy.Store.EFCore.Revisions;
using ReverseProxy.WebApi.Contracts.V1.Auth;
using ReverseProxy.WebApi.Contracts.V1.Configuration;
using ReverseProxy.WebApi.Contracts.V1.Runtime;
using ReverseProxy.WebApi.Data;
using Testcontainers.PostgreSql;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Store.Configuration;

namespace ReverseProxy.Store.Tests.WebApi;

public sealed class ControlPlaneApiTests(PostgreSqlFixture database) :
    IClassFixture<PostgreSqlFixture>,
    IDisposable
{
    private const string AdministratorName = "integration-admin";
    private const string AdministratorPassword = "ControlPlane.Test.2026!";

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
    public async Task AdministratorCanPublishValidatedDraftWithCsrfAndEtagConcurrency()
    {
        var anonymousResponse = await Client.GetAsync("/api/v1/drafts", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var anonymousToken = await GetAntiforgeryTokenAsync();
        var unprotectedLogin = await Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest
            {
                UserName = AdministratorName,
                Password = AdministratorPassword
            },
            CancellationToken.None);
        Assert.Equal(HttpStatusCode.BadRequest, unprotectedLogin.StatusCode);

        using (var loginRequest = CreateCsrfRequest(
                   HttpMethod.Post,
                   "/api/v1/auth/login",
                   anonymousToken,
                   new LoginRequest
                   {
                       UserName = AdministratorName,
                       Password = AdministratorPassword
                   }))
        {
            var loginResponse = await Client.SendAsync(loginRequest, CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            var user = await loginResponse.Content.ReadFromJsonAsync<CurrentUserResponse>(CancellationToken.None);
            Assert.NotNull(user);
            Assert.Contains("Administrator", user.Roles);
        }

        var authenticatedToken = await GetAntiforgeryTokenAsync();
        DraftResponse draft;
        string initialEtag;
        using (var createRequest = CreateCsrfRequest(
                   HttpMethod.Post,
                   "/api/v1/drafts",
                   authenticatedToken,
                   new CreateDraftRequest { Name = "Integration release" }))
        {
            var createResponse = await Client.SendAsync(createRequest, CancellationToken.None);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.NotNull(createResponse.Headers.Location);
            initialEtag = Assert.IsType<string>(createResponse.Headers.ETag?.Tag);
            Assert.Equal("\"1\"", initialEtag);
            draft = Assert.IsType<DraftResponse>(
                await createResponse.Content.ReadFromJsonAsync<DraftResponse>(CancellationToken.None));
        }

        var document = CreateDocument();
        using (var requestWithoutEtag = CreateCsrfRequest(
                   HttpMethod.Put,
                   $"/api/v1/drafts/{draft.Id}",
                   authenticatedToken,
                   new UpdateDraftRequest { Name = draft.Name, Document = document }))
        {
            var responseWithoutEtag = await Client.SendAsync(requestWithoutEtag, CancellationToken.None);
            Assert.Equal((HttpStatusCode)428, responseWithoutEtag.StatusCode);
        }

        using (var requestWithInvalidEtag = CreateCsrfRequest(
                   HttpMethod.Put,
                   $"/api/v1/drafts/{draft.Id}",
                   authenticatedToken,
                   new UpdateDraftRequest { Name = draft.Name, Document = document },
                   "1"))
        {
            var responseWithInvalidEtag = await Client.SendAsync(requestWithInvalidEtag, CancellationToken.None);
            Assert.Equal(HttpStatusCode.BadRequest, responseWithInvalidEtag.StatusCode);
        }

        string updatedEtag;
        using (var updateRequest = CreateCsrfRequest(
                   HttpMethod.Put,
                   $"/api/v1/drafts/{draft.Id}",
                   authenticatedToken,
                   new UpdateDraftRequest { Name = draft.Name, Document = document },
                   initialEtag))
        {
            var updateResponse = await Client.SendAsync(updateRequest, CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            updatedEtag = Assert.IsType<string>(updateResponse.Headers.ETag?.Tag);
            Assert.Equal("\"2\"", updatedEtag);
        }

        ConfigValidationResponse validation;
        using (var validateRequest = CreateCsrfRequest(
                   HttpMethod.Post,
                   $"/api/v1/drafts/{draft.Id}/validate",
                   authenticatedToken))
        {
            var validateResponse = await Client.SendAsync(validateRequest, CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
            validation = Assert.IsType<ConfigValidationResponse>(
                await validateResponse.Content.ReadFromJsonAsync<ConfigValidationResponse>(CancellationToken.None));
        }
        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);

        var diff = await Client.GetFromJsonAsync<ConfigDiffResponse>(
            $"/api/v1/drafts/{draft.Id}/diff",
            CancellationToken.None);
        Assert.NotNull(diff);
        Assert.True(diff.HasChanges);
        Assert.Equal(["orders-route"], diff.Routes.Added);
        Assert.Equal(["orders"], diff.Clusters.Added);

        ConfigOperationResponse publishOperation;
        Uri operationLocation;
        using (var publishRequest = CreateCsrfRequest(
                   HttpMethod.Post,
                   $"/api/v1/drafts/{draft.Id}/publish",
                   authenticatedToken,
                   etag: updatedEtag))
        {
            var publishResponse = await Client.SendAsync(publishRequest, CancellationToken.None);
            Assert.Equal(HttpStatusCode.Accepted, publishResponse.StatusCode);
            operationLocation = Assert.IsType<Uri>(publishResponse.Headers.Location);
            publishOperation = Assert.IsType<ConfigOperationResponse>(
                await publishResponse.Content.ReadFromJsonAsync<ConfigOperationResponse>(CancellationToken.None));
        }
        Assert.Equal("Applied", publishOperation.Status);
        Assert.Equal(1, publishOperation.RevisionNumber);

        var publishedDraftResponse = await Client.GetAsync($"/api/v1/drafts/{draft.Id}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, publishedDraftResponse.StatusCode);
        Assert.Equal("\"3\"", publishedDraftResponse.Headers.ETag?.Tag);
        var publishedDraft = Assert.IsType<DraftResponse>(
            await publishedDraftResponse.Content.ReadFromJsonAsync<DraftResponse>(CancellationToken.None));
        Assert.Equal(1, publishedDraft.BaseRevisionNumber);

        var operation = await Client.GetFromJsonAsync<ConfigOperationResponse>(
            operationLocation,
            CancellationToken.None);
        Assert.NotNull(operation);
        Assert.Equal(publishOperation.Id, operation.Id);
        Assert.Equal("Applied", operation.Status);

        var runtime = await Client.GetFromJsonAsync<RuntimeStatusResponse>(
            "/api/v1/runtime/status",
            CancellationToken.None);
        Assert.NotNull(runtime);
        Assert.Equal(1, runtime.RevisionNumber);
        Assert.Equal(1, runtime.RouteCount);
        Assert.Equal(1, runtime.ClusterCount);
        Assert.Equal("Healthy", runtime.Status);

        var runtimeConfiguration = await Client.GetFromJsonAsync<RuntimeConfigurationResponse>(
            "/api/v1/runtime/configuration",
            CancellationToken.None);
        Assert.NotNull(runtimeConfiguration);
        Assert.Equal(runtime.RevisionId, runtimeConfiguration.RevisionId);
        Assert.Equal(runtime.ContentHash, runtimeConfiguration.ContentHash);
        Assert.Equal("orders-route", Assert.Single(runtimeConfiguration.Document.Routes).RouteId);
        Assert.Equal("orders", Assert.Single(runtimeConfiguration.Document.Clusters).ClusterId);

        var readyResponse = await Client.GetAsync("/health/ready", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);

        await MoveActiveRevisionWithoutReloadingRuntimeAsync();

        var degradedRuntime = await Client.GetFromJsonAsync<RuntimeStatusResponse>(
            "/api/v1/runtime/status",
            CancellationToken.None);
        Assert.NotNull(degradedRuntime);
        Assert.Equal(1, degradedRuntime.RevisionNumber);
        Assert.Equal("Degraded", degradedRuntime.Status);

        var degradedReadyResponse = await Client.GetAsync("/health/ready", CancellationToken.None);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, degradedReadyResponse.StatusCode);
    }

    public void Dispose()
    {
        client?.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<AntiforgeryTokenResponse> GetAntiforgeryTokenAsync()
    {
        return Assert.IsType<AntiforgeryTokenResponse>(
            await Client.GetFromJsonAsync<AntiforgeryTokenResponse>(
                "/api/v1/auth/antiforgery",
                CancellationToken.None));
    }

    private async Task MoveActiveRevisionWithoutReloadingRuntimeAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var runtimeState = await dbContext.RuntimeConfigState.SingleAsync(CancellationToken.None);
        var document = CreateDocument();
        var revision = new ProxyConfigRevisionEntity
        {
            Id = Guid.NewGuid(),
            RevisionNumber = runtimeState.ActiveRevisionNumber + 1,
            SchemaVersion = document.SchemaVersion,
            ContentHash = ProxyConfigDocumentSerializer.ComputeHash(document),
            DocumentJson = ProxyConfigDocumentSerializer.SerializeCanonical(document),
            PublishedBy = "integration-test",
            PublishedAt = DateTimeOffset.UtcNow
        };
        dbContext.ConfigRevisions.Add(revision);
        runtimeState.ActiveRevision = revision;
        runtimeState.ActiveRevisionId = revision.Id;
        runtimeState.ActiveRevisionNumber = revision.RevisionNumber;
        runtimeState.ActiveContentHash = revision.ContentHash;
        runtimeState.UpdatedAt = revision.PublishedAt;
        await dbContext.SaveChangesAsync(CancellationToken.None);
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

    private static HttpRequestMessage CreateCsrfRequest(
        HttpMethod method,
        string path,
        AntiforgeryTokenResponse antiforgery,
        string? etag = null)
    {
        return CreateCsrfRequest<object>(method, path, antiforgery, etag: etag);
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
                    Match = new RouteMatch { Path = "/orders/{**catch-all}" },
                    Transforms =
                    [
                        new Dictionary<string, string> { ["PathRemovePrefix"] = "/orders" }
                    ]
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
}

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("reverseproxy_integration")
        .WithUsername("reverseproxy")
        .WithPassword("reverseproxy_test")
        .Build();

    public string ConnectionString => container.GetConnectionString();

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();
}

internal sealed class ControlPlaneWebApplicationFactory(
    string connectionString,
    string administratorName,
    string administratorPassword) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["BootstrapAdmin:UserName"] = administratorName,
                ["BootstrapAdmin:Password"] = administratorPassword,
                ["Logging:LogLevel:Default"] = "Warning"
            });
        });
    }
}