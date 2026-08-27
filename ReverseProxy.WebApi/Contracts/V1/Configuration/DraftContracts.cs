using System.ComponentModel.DataAnnotations;
using Yarp.ReverseProxy.Store.Configuration;

namespace ReverseProxy.WebApi.Contracts.V1.Configuration;

/// <summary>Payload for creating a configuration draft from the active revision.</summary>
public sealed record CreateDraftRequest
{
    [Required, StringLength(120, MinimumLength = 1)]
    public required string Name { get; init; }
}

/// <summary>Payload for replacing the contents of a configuration draft.</summary>
public sealed record UpdateDraftRequest
{
    [Required, StringLength(120, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required]
    public required ProxyConfigDocument Document { get; init; }
}

/// <summary>Summary information for a configuration draft.</summary>
public sealed record DraftSummaryResponse(
    Guid Id,
    string Name,
    long BaseRevisionNumber,
    long RowVersion,
    string ContentHash,
    int RouteCount,
    int ClusterCount,
    string UpdatedBy,
    DateTimeOffset UpdatedAt);

/// <summary>A configuration draft and its complete document.</summary>
public sealed record DraftResponse(
    Guid Id,
    string Name,
    long BaseRevisionNumber,
    long RowVersion,
    string ContentHash,
    string UpdatedBy,
    DateTimeOffset UpdatedAt,
    ProxyConfigDocument Document);

/// <summary>A field or cross-resource configuration validation error.</summary>
public sealed record ConfigValidationErrorResponse(string Code, string Path, string Message);

/// <summary>The authoritative validation result for a draft.</summary>
public sealed record ConfigValidationResponse(
    bool IsValid,
    string ContentHash,
    IReadOnlyList<ConfigValidationErrorResponse> Errors);

/// <summary>Resource IDs added, removed, or modified by a draft.</summary>
public sealed record ConfigResourceDiffResponse(
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Modified);

/// <summary>The resource-level difference between a draft and its base revision.</summary>
public sealed record ConfigDiffResponse(
    string BaseHash,
    string CandidateHash,
    bool HasChanges,
    ConfigResourceDiffResponse Routes,
    ConfigResourceDiffResponse Clusters);

/// <summary>A configuration publish or rollback operation.</summary>
public sealed record ConfigOperationResponse(
    Guid Id,
    string Type,
    string Status,
    Guid RevisionId,
    long RevisionNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorCode,
    string? ErrorMessage);
