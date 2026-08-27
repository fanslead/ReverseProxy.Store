namespace ReverseProxy.Store.EFCore.Revisions;

public interface IProxyConfigRevisionDbContext
{
    DbSet<ProxyConfigRevisionEntity> ConfigRevisions { get; }

    DbSet<RuntimeConfigStateEntity> RuntimeConfigState { get; }
}