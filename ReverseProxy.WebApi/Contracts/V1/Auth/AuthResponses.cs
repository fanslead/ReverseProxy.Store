namespace ReverseProxy.WebApi.Contracts.V1.Auth;

/// <summary>Antiforgery data required for state-changing API requests.</summary>
public sealed record AntiforgeryTokenResponse(string RequestToken, string HeaderName);

/// <summary>The authenticated control-plane user.</summary>
public sealed record CurrentUserResponse(Guid Id, string UserName, string DisplayName, IReadOnlyList<string> Roles);