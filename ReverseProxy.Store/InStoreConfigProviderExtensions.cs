using Microsoft.Extensions.Logging;
using Microsoft.ReverseProxy.Service;
using Microsoft.ReverseProxy.Store;

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
