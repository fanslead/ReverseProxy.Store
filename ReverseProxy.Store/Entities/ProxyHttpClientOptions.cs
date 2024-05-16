using ReverseProxy.Store.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;

namespace ReverseProxy.Store.Entities
{
    public class HttpClientConfig
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// An empty options instance.
        /// </summary>
        public static readonly HttpClientConfig Empty = new();

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
        /// Limits the number of connections used when communicating with the destination server.
        /// </summary>
        public int? MaxConnectionsPerServer { get; init; }

        /// <summary>
        /// Optional web proxy used when communicating with the destination server. 
        /// </summary>
        public virtual WebProxyConfig? WebProxy { get; init; }

#if NET
        /// <summary>
        /// Gets or sets a value that indicates whether additional HTTP/2 connections can
        /// be established to the same server when the maximum number of concurrent streams
        /// is reached on all existing connections.
        /// </summary>
        public bool? EnableMultipleHttp2Connections { get; init; }

        /// <summary>
        /// Enables non-ASCII header encoding for outgoing requests.
        /// </summary>
        public string? RequestHeaderEncoding { get; init; }
#endif

        public string ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }
        public bool Equals(HttpClientConfig? other)
        {
            if (other == null)
            {
                return false;
            }

            return SslProtocols == other.SslProtocols
                   && DangerousAcceptAnyServerCertificate == other.DangerousAcceptAnyServerCertificate
                   && MaxConnectionsPerServer == other.MaxConnectionsPerServer
#if NET
                   && EnableMultipleHttp2Connections == other.EnableMultipleHttp2Connections
                   // Comparing by reference is fine here since Encoding.GetEncoding returns the same instance for each encoding.
                   && RequestHeaderEncoding == other.RequestHeaderEncoding
#endif
                   && WebProxy == other.WebProxy;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SslProtocols,
                DangerousAcceptAnyServerCertificate,
                MaxConnectionsPerServer,
#if NET
                EnableMultipleHttp2Connections,
                RequestHeaderEncoding,
#endif
                WebProxy);
        }
    }
}
