namespace ReverseProxy.WebApi.Application.Configuration;

public sealed class ConfigOperationRecoveryOptions
{
    public const string SectionName = "Operations";

    public int BatchSize { get; set; } = 32;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
}