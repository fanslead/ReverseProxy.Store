using System.ComponentModel.DataAnnotations;

namespace ReverseProxy.Store.Entity
{
    public class RequestProxyOptions
    {
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// The time allowed to send the request and receive the response headers. This may include
        /// the time needed to send the request body. The default is 100 seconds.
        /// </summary>
        public string Timeout { get; set; }

        /// <summary>
        /// Preferred version of the outgoing request.
        /// The default is HTTP/2.0.
        /// </summary>
        public string Version { get; set; }

#if NET
        /// <summary>
        /// The policy applied to version selection, e.g. whether to prefer downgrades, upgrades or
        /// request an exact version. The default is `RequestVersionOrLower`.
        /// </summary>
        public string VersionPolicy { get; init; }
#endif
        public string ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }
    }
}
