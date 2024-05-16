namespace ReverseProxy.Store.Entities;

public class PassiveHealthCheckOptions
{
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// Whether passive health checks are enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Passive health check policy.
    /// </summary>
    public string Policy { get; set; }

    /// <summary>
    /// Destination reactivation period after which an unhealthy destination is considered healthy again.
    /// </summary>
    public string ReactivationPeriod { get; set; }
    public int HealthCheckOptionsId { get; set; }

    public virtual HealthCheckOptions HealthCheckOptions { get; set; }
}
