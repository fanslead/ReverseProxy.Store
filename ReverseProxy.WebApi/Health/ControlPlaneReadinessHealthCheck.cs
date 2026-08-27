using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReverseProxy.Store.EFCore.Revisions;
using ReverseProxy.WebApi.Data;
using Yarp.ReverseProxy.Store.Revisions;

namespace ReverseProxy.WebApi.Health;

public sealed class ControlPlaneReadinessHealthCheck(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    RevisionedProxyConfigProvider configProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var runtimeState = await dbContext.RuntimeConfigState
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);
            if (runtimeState is null)
            {
                return HealthCheckResult.Unhealthy("The runtime configuration state is not initialized.");
            }

            var config = (RevisionedProxyConfig)configProvider.GetConfig();
            var healthData = new Dictionary<string, object>
            {
                ["activeRevisionNumber"] = runtimeState.ActiveRevisionNumber,
                ["appliedRevisionNumber"] = config.RevisionNumber,
                ["activeContentHash"] = runtimeState.ActiveContentHash,
                ["appliedContentHash"] = config.ContentHash
            };
            if (runtimeState.ActiveRevisionId != config.RevisionId
                || runtimeState.ActiveRevisionNumber != config.RevisionNumber
                || !string.Equals(runtimeState.ActiveContentHash, config.ContentHash, StringComparison.OrdinalIgnoreCase))
            {
                return HealthCheckResult.Unhealthy(
                    "The runtime node has not applied the active configuration revision.",
                    data: healthData);
            }

            return HealthCheckResult.Healthy(
                "The control plane and runtime configuration are ready.",
                healthData);
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The control plane is not ready.", exception);
        }
    }
}