using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReverseProxy.WebApi.Application.Configuration;
using ReverseProxy.WebApi.Contracts.V1.Configuration;

namespace ReverseProxy.WebApi.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/operations")]
public sealed class OperationsController(IConfigDraftService draftService) : ControllerBase
{
    [HttpGet("{operationId:guid}")]
    [ProducesResponseType<ConfigOperationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfigOperationResponse>> Get(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        return Ok(await draftService.GetOperationAsync(operationId, cancellationToken));
    }
}