using System.ComponentModel.DataAnnotations;

namespace ReverseProxy.Store.EFCore
{
    public class ProxyHttpClientOptions
    {
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// What TLS protocols to use.
        /// </summary>
        public string SslProtocols { get; init; }

        /// <summary>
        /// Indicates if destination server https certificate errors should be ignored.
        /// This should only be done when using self-signed certificates.
        /// </summary>
        public bool? DangerousAcceptAnyServerCertificate { get; init; }

        /// <summary>
        /// A client certificate used to authenticate to the destination server.
        /// </summary>
        public string ClientCertificate { get; init; }

        /// <summary>
        /// Limits the number of connections used when communicating with the destination server.
        /// </summary>
        public int? MaxConnectionsPerServer { get; init; }

        /// <summary>
        /// Specifies the activity correlation headers for outgoing requests.
        /// </summary>
        public string ActivityContextHeaders { get; init; }

#if NET
        /// <summary>
        /// Gets or sets a value that indicates whether additional HTTP/2 connections can
        //  be established to the same server when the maximum number of concurrent streams
        //  is reached on all existing connections.
        /// </summary>
        public bool? EnableMultipleHttp2Connections { get; init; }
#endif
        public string ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }
    }
}
