using Microsoft.Extensions.Primitives;
using Microsoft.ReverseProxy.Service;

namespace Microsoft.ReverseProxy.Store
{
    public interface IReverseProxyStore
    {
        IProxyConfig GetConfig();

        void Reload();
        IChangeToken GetReloadToken();
    }
}
