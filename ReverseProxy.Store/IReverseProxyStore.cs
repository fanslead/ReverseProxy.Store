using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Yarp.ReverseProxy.Store
{
    public interface IReverseProxyStore
    {
        public event ConfigChangeHandler ChangeConfig;
        IProxyConfig GetConfig();

        void Reload();
        void ReloadConfig();
        IChangeToken GetReloadToken();
    }
}
