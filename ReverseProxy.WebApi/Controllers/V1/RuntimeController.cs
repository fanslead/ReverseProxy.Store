using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReverseProxy.WebApi.Contracts.V1.Runtime;
using Yarp.ReverseProxy.Store.Revisions;

namespace ReverseProxy.WebApi.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/runtime")]
public sealed class RuntimeController(
    RevisionedProxyConfigProvider configProvider,
    IProxyConfigRevisionStore revisionStore) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<RuntimeStatusResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RuntimeStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var config = (RevisionedProxyConfig)configProvider.GetConfig();
        var activeRevision = await revisionStore.LoadActiveRevisionAsync(cancellationToken);
        var status = config.RevisionId == activeRevision.RevisionId
            && config.RevisionNumber == activeRevision.RevisionNumber
            && string.Equals(config.ContentHash, activeRevision.ContentHash, StringComparison.OrdinalIgnoreCase)
                ? "Healthy"
                : "Degraded";
        return Ok(new RuntimeStatusResponse(
            config.RevisionId,
            config.RevisionNumber,
            config.ContentHash,
            config.PublishedAt,
            config.LoadedAt,
            config.Routes.Count,
            config.Clusters.Count,
            status));
    }

    [HttpGet("configuration")]
    [ProducesResponseType<RuntimeConfigurationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RuntimeConfigurationResponse>> GetConfiguration(
        CancellationToken cancellationToken)
    {
        var revision = await revisionStore.LoadActiveRevisionAsync(cancellationToken);
        return Ok(new RuntimeConfigurationResponse(
            revision.RevisionId,
            revision.RevisionNumber,
            revision.ContentHash,
            revision.PublishedAt,
            revision.Document));
    }
}