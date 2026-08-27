using System.ComponentModel.DataAnnotations;
using Yarp.ReverseProxy.Store.Configuration;

namespace ReverseProxy.WebApi.Contracts.V1.Configuration;

/// <summary>Summary information for an immutable configuration revision.</summary>
public sealed record RevisionSummaryResponse(
    Guid Id,
    long RevisionNumber,
    int SchemaVersion,
    string ContentHash,
    string PublishedBy,
    DateTimeOffset PublishedAt,
    bool IsActive);

/// <summary>An immutable configuration revision and its complete document.</summary>
public sealed record RevisionResponse(
    Guid Id,
    long RevisionNumber,
    int SchemaVersion,
    string ContentHash,
    string PublishedBy,
    DateTimeOffset PublishedAt,
    bool IsActive,
    ProxyConfigDocument Document);

/// <summary>Payload for restoring a historical revision as a new active revision.</summary>
public sealed record RollbackRevisionRequest
{
    [Required]
    public required Guid? ExpectedActiveRevisionId { get; init; }
}