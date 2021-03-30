using ReverseProxy.Store.Entity;
using System.Security.Cryptography.X509Certificates;

namespace ReverseProxy.Store
{
    public interface ICertificateConfigLoader
    {
        X509Certificate2 LoadCertificate(CertificateConfig certificateConfig);
    }
}
