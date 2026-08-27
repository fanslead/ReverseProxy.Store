using ReverseProxy.WebApi.Application.Common;
using ReverseProxy.WebApi.Contracts.V1.Common;
using ReverseProxy.WebApi.Contracts.V1.Configuration;

namespace ReverseProxy.WebApi.Application.Configuration;

public interface IConfigRevisionService
{
    Task<PagedResponse<RevisionSummaryResponse>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<RevisionResponse> GetAsync(Guid revisionId, CancellationToken cancellationToken);

    Task<ConfigOperationResponse> RollbackAsync(
        Guid revisionId,
        Guid expectedActiveRevisionId,
        RequestActor actor,
        CancellationToken cancellationToken);
}