namespace ReverseProxy.WebApi.Application.Common;

public sealed record RequestActor
{
	public RequestActor(string id, string? ipAddress, string? userAgent)
	{
		Id = Truncate(id, 256) ?? "unknown";
		IpAddress = Truncate(ipAddress, 64);
		UserAgent = Truncate(userAgent, 512);
	}

	public string Id { get; }

	public string? IpAddress { get; }

	public string? UserAgent { get; }

	private static string? Truncate(string? value, int maximumLength) =>
		value is null || value.Length <= maximumLength ? value : value[..maximumLength];
}