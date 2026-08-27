using System.ComponentModel.DataAnnotations;

namespace ReverseProxy.WebApi.Contracts.V1.Auth;

/// <summary>Credentials used to start a control-plane session.</summary>
public sealed record LoginRequest
{
    [Required, StringLength(256)]
    public required string UserName { get; init; }

    [Required, StringLength(1024)]
    public required string Password { get; init; }

    public bool RememberMe { get; init; }
}