using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Store;
using Yarp.ReverseProxy.Service;
using ReverseProxy.Store.Entity;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InStoreConfigProviderExtensions
    {
        public static IReverseProxyBuilder LoadFromStore(this IReverseProxyBuilder builder)
        {
            builder.Services.AddSingleton<IProxyConfigProvider>(sp =>
            {
                return new InStoreConfigProvider(sp.GetService<ILogger<InStoreConfigProvider>>(), sp.GetRequiredService<IReverseProxyStore>());
            });
            return builder;
        }
    }
}
