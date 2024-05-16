namespace ReverseProxy.Store.EFCore;

public static class ReverseProxyStoreEFCoreExtensions
{
    public static IReverseProxyBuilder LoadFromEFCore(this IReverseProxyBuilder builder)
    {
        builder.Services.AddSingleton<IReverseProxyStore, EFCoreReverseProxyStore>();
        builder.LoadFromStore();
        return builder;
    }
}
