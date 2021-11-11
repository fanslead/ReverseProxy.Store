using Microsoft.Extensions.DependencyInjection;
using ReverseProxy.Store.Entity;
using Yarp.ReverseProxy.Store;

namespace ReverseProxy.Store.EFCore
{
    public static class ReverseProxyStoreEFCoreExtensions
    {
        public static IReverseProxyBuilder LoadFromEFCore(this IReverseProxyBuilder builder)
        {
            builder.Services.AddSingleton<IReverseProxyStore, EFCoreReverseProxyStore>();
            builder.LoadFromStore();
            return builder;
        }
    }
}
