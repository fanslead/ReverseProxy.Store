using FluentValidation;
using ReverseProxy.Store.Entities;
using System.Globalization;
using System.Security.Authentication;

namespace ReverseProxy.WebApi.Validator
{
    public class ClusterValidator : AbstractValidator<Cluster>
    {
        public ClusterValidator()
        {
            RuleFor(x => x.HealthCheck.Passive.ReactivationPeriod)
                .Must(period =>
                {
                    if (!string.IsNullOrWhiteSpace(period))
                    {
                        return TimeSpan.TryParse(period, CultureInfo.InvariantCulture, out TimeSpan result);
                    }
                    return true;
                })
                .When(x => x.HealthCheck != null && x.HealthCheck.Passive != null)
                .WithMessage("Cluster.HealthCheck.Passive.ReactivationPeriod Must Format 00:00:00");
            ;
            RuleFor(x => x.HealthCheck.Active.Interval)
                .Must(interval =>
                {
                    if (!string.IsNullOrWhiteSpace(interval))
                    {
                        return TimeSpan.TryParse(interval, CultureInfo.InvariantCulture, out TimeSpan result);
                    }
                    return true;
                })
                .When(x => x.HealthCheck != null && x.HealthCheck.Active != null)
                .WithMessage("Cluster.HealthCheck.Active.Interval Must Format 00:00:00");
            ;
            RuleFor(x => x.HealthCheck.Active.Timeout)
                .Must(timeout =>
                {
                    if (!string.IsNullOrWhiteSpace(timeout))
                    {
                        return TimeSpan.TryParse(timeout, CultureInfo.InvariantCulture, out TimeSpan result);
                    }
                    return true;
                })
                .When(x => x.HealthCheck != null && x.HealthCheck.Active != null)
                .WithMessage("Cluster.HealthCheck.Active.Timeout Must Format 00:00:00");
            ;
            RuleFor(x => x.HttpClient.SslProtocols)
                .Must(sslProtocols =>
                {
                    if (!string.IsNullOrWhiteSpace(sslProtocols))
                    {
                        var sslProtocolArr = sslProtocols.Split(",");
                        if (sslProtocolArr.Length > 0)
                        {
                            foreach (var sslProtocol in sslProtocolArr)
                            {
                                var isRight = Enum.TryParse<SslProtocols>(sslProtocol, ignoreCase: true, out SslProtocols result);
                                if (!isRight)
                                    return false;
                            }
                        }
                    }
                    return true;
                })
                .When(x => x.HttpClient != null)
                .WithMessage("Cluster.HttpClient.SslProtocols Must in None|Ssl2|Ssl3|Default|Tls|Tls11|Tls12|Tls13");
            ;
            RuleFor(x => x.HttpRequest.ActivityTimeout)
                .Must(timeout =>
                {
                    if (!string.IsNullOrWhiteSpace(timeout))
                    {
                        return TimeSpan.TryParse(timeout, CultureInfo.InvariantCulture, out TimeSpan result);
                    }
                    return true;
                })
                .When(x => x.HttpRequest != null)
                .WithMessage("Cluster.HttpRequest.Timeout Must Format 00:00:00");
            ;
            RuleFor(x => x.HttpRequest.Version)
                .Must(value =>
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return Version.TryParse(value + (value.Contains('.') ? "" : ".0"), out Version? result);
                    }
                    return true;
                })
                .When(x => x.HttpRequest != null)
                .WithMessage("Cluster.HttpRequest.Version Format error");
            ;
            RuleFor(x => x.HttpRequest.VersionPolicy)
                .Must(versionPolicy =>
                {
                    if (!string.IsNullOrWhiteSpace(versionPolicy))
                    {
                        return Enum.TryParse<HttpVersionPolicy>(versionPolicy, ignoreCase: true, out HttpVersionPolicy result);
                    }
                    return true;
                })
                .When(x => x.HttpRequest != null)
                .WithMessage("Cluster.HttpRequest.VersionPolicy Must in RequestVersionOrLower|RequestVersionOrHigher|RequestVersionExact");
            ;
        }
    }
}
