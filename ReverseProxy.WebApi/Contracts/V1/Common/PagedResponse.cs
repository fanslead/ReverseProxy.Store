namespace ReverseProxy.WebApi.Contracts.V1.Common;

/// <summary>A stable page of API resources.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);