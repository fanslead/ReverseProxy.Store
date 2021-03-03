using System.ComponentModel.DataAnnotations;

namespace ReverseProxy.Store.EFCore
{
    public class HealthCheckOptions
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Passive health check options.
        /// </summary>
        public virtual PassiveHealthCheckOptions Passive { get; init; }

        /// <summary>
        /// Active health check options.
        /// </summary>
        public virtual ActiveHealthCheckOptions Active { get; init; }
        public string ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }
    }
}
