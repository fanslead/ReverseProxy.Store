using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Yarp.ReverseProxy.Store
{
    public interface IReverseProxyStore
    {
        IProxyConfig GetConfig();

        void Reload();
        IChangeToken GetReloadToken();
    }
}
