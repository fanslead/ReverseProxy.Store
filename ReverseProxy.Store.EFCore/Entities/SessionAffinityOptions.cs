using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ReverseProxy.Store.EFCore
{
    public class SessionAffinityOptions
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Indicates whether session affinity is enabled.
        /// </summary>
        public bool? Enabled { get; init; }

        /// <summary>
        /// Session affinity mode which is implemented by one of providers.
        /// </summary>
        public string Mode { get; init; }

        /// <summary>
        /// Strategy handling missing destination for an affinitized request.
        /// </summary>
        public string FailurePolicy { get; init; }

        /// <summary>
        /// Key-value pair collection holding extra settings specific to different affinity modes.
        /// </summary>
        public virtual List<SessionAffinityOptionSetting> Settings { get; init; }
        public string ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }
    }
}
