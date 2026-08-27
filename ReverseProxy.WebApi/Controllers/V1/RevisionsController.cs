using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReverseProxy.WebApi.Application.Common;
using ReverseProxy.WebApi.Application.Configuration;
using ReverseProxy.WebApi.Contracts.V1.Common;
using ReverseProxy.WebApi.Contracts.V1.Configuration;

namespace ReverseProxy.WebApi.Controllers.V1;

[ApiController]
[Authorize(Policy = "CanView")]
[Route("api/v1/revisions")]
public sealed class RevisionsController(IConfigRevisionService revisionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<RevisionSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<RevisionSummaryResponse>>> List(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        return Ok(await revisionService.ListAsync(page, pageSize, cancellationToken));
    }

    [HttpGet("{revisionId:guid}")]
    [ProducesResponseType<RevisionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RevisionResponse>> Get(
        Guid revisionId,
        CancellationToken cancellationToken)
    {
        return Ok(await revisionService.GetAsync(revisionId, cancellationToken));
    }

    [Authorize(Policy = "CanPublish")]
    [EnableRateLimiting("publish")]
    [HttpPost("{revisionId:guid}/rollback")]
    [ProducesResponseType<ConfigOperationResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ConfigOperationResponse>> Rollback(
        Guid revisionId,
        RollbackRevisionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await revisionService.RollbackAsync(
            revisionId,
            request.ExpectedActiveRevisionId!.Value,
            GetActor(),
            cancellationToken);
        Response.Headers.Location = $"/api/v1/operations/{response.Id}";
        return Accepted(response);
    }

    private RequestActor GetActor() =>
        new(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "unknown",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
}