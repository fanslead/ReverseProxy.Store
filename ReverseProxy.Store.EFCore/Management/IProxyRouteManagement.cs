namespace ReverseProxy.Store.EFCore.Management;

public interface IProxyRouteManagement
{
    IQueryable<ProxyRoute> GetAll();
    Task<ProxyRoute> Find(int id);
    Task<bool> Create(ProxyRoute proxyRoute);
    Task<bool> Update(ProxyRoute proxyRoute);
    Task<bool> Delete(int id);
}
