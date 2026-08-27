namespace ReverseProxy.WebApi.Application.Common;

public sealed record RequestActor(string Id, string? IpAddress, string? UserAgent);