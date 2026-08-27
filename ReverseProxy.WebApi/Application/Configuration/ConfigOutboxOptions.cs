namespace ReverseProxy.WebApi.Application.Configuration;

public sealed class ConfigOutboxOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; set; } = 32;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan FailureDelay { get; set; } = TimeSpan.FromSeconds(5);
}