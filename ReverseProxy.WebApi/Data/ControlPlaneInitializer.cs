using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReverseProxy.Store.EFCore.Revisions;
using ReverseProxy.WebApi.Security;
using Yarp.ReverseProxy.Store.Configuration;

namespace ReverseProxy.WebApi.Data;

public static partial class ControlPlaneInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ControlPlaneInitializer");

        if (configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            throw new InvalidOperationException(
                "The control-plane database has pending migrations. Apply them before starting the application.");
        }

        await EnsureInitialRevisionAsync(dbContext, cancellationToken);
        await EnsureAdministratorAsync(scope.ServiceProvider, configuration, environment, logger);
    }

    private static async Task EnsureInitialRevisionAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.RuntimeConfigState.AnyAsync(cancellationToken))
        {
            return;
        }

        var document = new ProxyConfigDocument();
        var now = DateTimeOffset.UtcNow;
        var revision = new ProxyConfigRevisionEntity
        {
            Id = Guid.NewGuid(),
            RevisionNumber = 0,
            SchemaVersion = document.SchemaVersion,
            ContentHash = ProxyConfigDocumentSerializer.ComputeHash(document),
            DocumentJson = ProxyConfigDocumentSerializer.SerializeCanonical(document),
            PublishedBy = "system",
            PublishedAt = now
        };
        dbContext.ConfigRevisions.Add(revision);
        dbContext.RuntimeConfigState.Add(new RuntimeConfigStateEntity
        {
            ActiveRevision = revision,
            ActiveRevisionId = revision.Id,
            ActiveRevisionNumber = revision.RevisionNumber,
            ActiveContentHash = revision.ContentHash,
            UpdatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAdministratorAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var roleName in ControlPlaneRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                EnsureSucceeded(roleResult, $"create role '{roleName}'");
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.Users.AnyAsync())
        {
            return;
        }

        var userName = configuration["BootstrapAdmin:UserName"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "No administrator exists. Configure BootstrapAdmin__UserName and BootstrapAdmin__Password as deployment secrets.");
            }

            LogBootstrapSkipped(logger);
            return;
        }

        var administrator = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            DisplayName = userName
        };
        EnsureSucceeded(await userManager.CreateAsync(administrator, password), "create bootstrap administrator");
        EnsureSucceeded(
            await userManager.AddToRolesAsync(administrator, ControlPlaneRoles.All),
            "assign bootstrap administrator roles");
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Failed to {operation}. {errors}");
    }

    [LoggerMessage(1, LogLevel.Warning, "No bootstrap administrator credentials were configured; no administrator was created.")]
    private static partial void LogBootstrapSkipped(ILogger logger);
}