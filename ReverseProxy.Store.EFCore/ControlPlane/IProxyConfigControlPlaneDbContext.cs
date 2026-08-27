using ReverseProxy.Store.EFCore.Revisions;

namespace ReverseProxy.Store.EFCore.ControlPlane;

public interface IProxyConfigControlPlaneDbContext : IProxyConfigRevisionDbContext
{
    DbSet<ProxyConfigDraftEntity> ConfigDrafts { get; }

    DbSet<ConfigOperationEntity> ConfigOperations { get; }

    DbSet<ConfigOutboxMessageEntity> ConfigOutboxMessages { get; }

    DbSet<AuditEntryEntity> AuditEntries { get; }

    DbSet<RuntimeNodeEntity> RuntimeNodes { get; }
}