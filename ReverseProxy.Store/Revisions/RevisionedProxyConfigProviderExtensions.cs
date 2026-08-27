using Yarp.ReverseProxy.Store.Revisions;

namespace Microsoft.Extensions.DependencyInjection;

public static class RevisionedProxyConfigProviderExtensions
{
    public static IReverseProxyBuilder LoadFromRevisionStore(this IReverseProxyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<RevisionedProxyConfigProvider>();
        builder.Services.AddSingleton<IProxyConfigProvider>(
            serviceProvider => serviceProvider.GetRequiredService<RevisionedProxyConfigProvider>());
        return builder;
    }
}