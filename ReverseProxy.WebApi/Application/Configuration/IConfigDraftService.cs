using ReverseProxy.WebApi.Application.Common;
using ReverseProxy.WebApi.Contracts.V1.Common;
using ReverseProxy.WebApi.Contracts.V1.Configuration;

namespace ReverseProxy.WebApi.Application.Configuration;

public interface IConfigDraftService
{
    Task<PagedResponse<DraftSummaryResponse>> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DraftResponse> GetAsync(Guid draftId, CancellationToken cancellationToken);

    Task<DraftResponse> CreateAsync(CreateDraftRequest request, RequestActor actor, CancellationToken cancellationToken);

    Task<DraftResponse> UpdateAsync(
        Guid draftId,
        UpdateDraftRequest request,
        long expectedRowVersion,
        RequestActor actor,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid draftId, long expectedRowVersion, RequestActor actor, CancellationToken cancellationToken);

    Task<ConfigValidationResponse> ValidateAsync(Guid draftId, CancellationToken cancellationToken);

    Task<ConfigDiffResponse> DiffAsync(Guid draftId, CancellationToken cancellationToken);

    Task<ConfigOperationResponse> PublishAsync(
        Guid draftId,
        long expectedRowVersion,
        RequestActor actor,
        CancellationToken cancellationToken);

    Task<ConfigOperationResponse> GetOperationAsync(Guid operationId, CancellationToken cancellationToken);
}