using ReverseProxy.Store.EFCore.Revisions;
using Yarp.ReverseProxy.Store.Revisions;

namespace Microsoft.Extensions.DependencyInjection;

public static class EFCoreProxyConfigRevisionStoreExtensions
{
    public static IReverseProxyBuilder LoadFromEFCoreRevisionStore<TContext>(this IReverseProxyBuilder builder)
        where TContext : DbContext, IProxyConfigRevisionDbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IProxyConfigRevisionStore, EFCoreProxyConfigRevisionStore<TContext>>();
        return builder.LoadFromRevisionStore();
    }
}