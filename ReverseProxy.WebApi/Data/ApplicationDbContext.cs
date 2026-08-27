using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReverseProxy.Store.EFCore.ControlPlane;
using ReverseProxy.Store.EFCore.Revisions;

namespace ReverseProxy.WebApi.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options),
        IProxyConfigControlPlaneDbContext,
        IDataProtectionKeyContext
{
    public DbSet<ProxyConfigRevisionEntity> ConfigRevisions => Set<ProxyConfigRevisionEntity>();

    public DbSet<RuntimeConfigStateEntity> RuntimeConfigState => Set<RuntimeConfigStateEntity>();

    public DbSet<ProxyConfigDraftEntity> ConfigDrafts => Set<ProxyConfigDraftEntity>();

    public DbSet<ConfigOperationEntity> ConfigOperations => Set<ConfigOperationEntity>();

    public DbSet<ConfigOutboxMessageEntity> ConfigOutboxMessages => Set<ConfigOutboxMessageEntity>();

    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();

    public DbSet<RuntimeNodeEntity> RuntimeNodes => Set<RuntimeNodeEntity>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureProxyConfigControlPlane();

        builder.Entity<ApplicationUser>().ToTable("auth_users");
        builder.Entity<IdentityRole<Guid>>().ToTable("auth_roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("auth_user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("auth_user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("auth_user_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("auth_role_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("auth_user_tokens");
        builder.Entity<DataProtectionKey>().ToTable("data_protection_keys");

        builder.Entity<ProxyConfigRevisionEntity>()
            .Property(entity => entity.DocumentJson)
            .HasColumnType("jsonb");
        builder.Entity<ProxyConfigDraftEntity>()
            .Property(entity => entity.DocumentJson)
            .HasColumnType("jsonb");
        builder.Entity<ConfigOutboxMessageEntity>()
            .Property(entity => entity.PayloadJson)
            .HasColumnType("jsonb");
        builder.Entity<AuditEntryEntity>()
            .Property(entity => entity.DetailsJson)
            .HasColumnType("jsonb");
    }
}