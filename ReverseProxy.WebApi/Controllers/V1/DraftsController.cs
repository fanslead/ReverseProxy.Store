using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using ReverseProxy.WebApi.Application.Common;
using ReverseProxy.WebApi.Application.Configuration;
using ReverseProxy.WebApi.Contracts.V1.Common;
using ReverseProxy.WebApi.Contracts.V1.Configuration;
using ReverseProxy.WebApi.Infrastructure.Http;

namespace ReverseProxy.WebApi.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/drafts")]
public sealed class DraftsController(IConfigDraftService draftService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<DraftSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DraftSummaryResponse>>> List(
        [FromQuery] string? search,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        return Ok(await draftService.ListAsync(search, page, pageSize, cancellationToken));
    }

    [HttpGet("{draftId:guid}")]
    [ProducesResponseType<DraftResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DraftResponse>> Get(Guid draftId, CancellationToken cancellationToken)
    {
        var response = await draftService.GetAsync(draftId, cancellationToken);
        Response.Headers.ETag = DraftEntityTag.Create(response.RowVersion);
        return Ok(response);
    }

    [Authorize(Policy = "CanEdit")]
    [HttpPost]
    [ProducesResponseType<DraftResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DraftResponse>> Create(
        CreateDraftRequest request,
        CancellationToken cancellationToken)
    {
        var response = await draftService.CreateAsync(request, GetActor(), cancellationToken);
        Response.Headers.ETag = DraftEntityTag.Create(response.RowVersion);
        return CreatedAtAction(nameof(Get), new { draftId = response.Id }, response);
    }

    [Authorize(Policy = "CanEdit")]
    [HttpPut("{draftId:guid}")]
    [ProducesResponseType<DraftResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status428PreconditionRequired)]
    public async Task<ActionResult<DraftResponse>> Update(
        Guid draftId,
        UpdateDraftRequest request,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = DraftEntityTag.ParseRequired(Request.Headers);
        var response = await draftService.UpdateAsync(
            draftId,
            request,
            expectedRowVersion,
            GetActor(),
            cancellationToken);
        Response.Headers.ETag = DraftEntityTag.Create(response.RowVersion);
        return Ok(response);
    }

    [Authorize(Policy = "CanEdit")]
    [HttpDelete("{draftId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid draftId, CancellationToken cancellationToken)
    {
        await draftService.DeleteAsync(
            draftId,
            DraftEntityTag.ParseRequired(Request.Headers),
            GetActor(),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{draftId:guid}/validate")]
    [ProducesResponseType<ConfigValidationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigValidationResponse>> Validate(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        return Ok(await draftService.ValidateAsync(draftId, cancellationToken));
    }

    [HttpGet("{draftId:guid}/diff")]
    [ProducesResponseType<ConfigDiffResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigDiffResponse>> Diff(Guid draftId, CancellationToken cancellationToken)
    {
        return Ok(await draftService.DiffAsync(draftId, cancellationToken));
    }

    [Authorize(Policy = "CanPublish")]
    [EnableRateLimiting("publish")]
    [HttpPost("{draftId:guid}/publish")]
    [ProducesResponseType<ConfigOperationResponse>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<ConfigOperationResponse>> Publish(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var response = await draftService.PublishAsync(
            draftId,
            DraftEntityTag.ParseRequired(Request.Headers),
            GetActor(),
            cancellationToken);
        Response.Headers.Location = $"/api/v1/operations/{response.Id}";
        return Accepted(response);
    }

    private RequestActor GetActor()
    {
        return new RequestActor(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "unknown",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
    }
}