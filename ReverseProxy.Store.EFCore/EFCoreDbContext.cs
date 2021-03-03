using Microsoft.EntityFrameworkCore;

namespace ReverseProxy.Store.EFCore
{
    public class EFCoreDbContext : DbContext, IReverseProxyStoreDbContext
    {

        public EFCoreDbContext(DbContextOptions<EFCoreDbContext> options) : base(options)
        {
        }

        public DbSet<Cluster> Clusters { get; set; }
        public DbSet<ProxyRoute> ProxyRoutes { get; set; }
        public DbSet<Destination> Destinations { get; set; }
        public DbSet<ActiveHealthCheckOptions> ActiveHealthCheckOptions { get; set; }
        public DbSet<HealthCheckOptions> HealthCheckOptions { get; set; }
        public DbSet<Metadata> Metadatas { get; set; }
        public DbSet<PassiveHealthCheckOptions> PassiveHealthCheckOptions { get; set; }
        public DbSet<ProxyHttpClientOptions> ProxyHttpClientOptions { get; set; }
        public DbSet<ProxyMatch> ProxyMatches { get; set; }
        public DbSet<RequestProxyOptions> RequestProxyOptions { get; set; }
        public DbSet<RouteHeader> RouteHeaders { get; set; }
        public DbSet<SessionAffinityOptions> SessionAffinityOptions { get; set; }
        public DbSet<SessionAffinityOptionSetting> SessionAffinityOptionSettings { get; set; }
        public DbSet<Transform> Transforms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            BuildCluster(modelBuilder);
            BuildDestination(modelBuilder);
            BuildSessionAffinityOptions(modelBuilder);
            BuildHealthCheckOptions(modelBuilder);
            BuildProxyRoute(modelBuilder);
            BuildProxyMatch(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }
        private void BuildCluster(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cluster>(builder =>
            {
                builder
                   .HasMany(p => p.Destinations)
                   .WithOne()
                   .OnDelete(DeleteBehavior.Cascade);
                builder
                    .HasMany(p => p.Metadata)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);
                builder
                    .HasOne(p => p.SessionAffinity).WithOne(s => s.Cluster)
                    .OnDelete(DeleteBehavior.Cascade);
                builder
                    .HasOne(p => p.HealthCheck).WithOne(s => s.Cluster)
                    .OnDelete(DeleteBehavior.Cascade);
                builder
                    .HasOne(p => p.HttpClient).WithOne(s => s.Cluster)
                    .OnDelete(DeleteBehavior.Cascade);
                builder
                    .HasOne(p => p.HttpRequest).WithOne(s => s.Cluster)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
        private void BuildDestination(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Destination>(builder =>
            {
                builder
                .HasMany(p => p.Metadata)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            });
        }
        private void BuildSessionAffinityOptions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SessionAffinityOptions>(builder =>
            {
                builder
                .HasMany(p => p.Settings)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
            });
        }
        private void BuildHealthCheckOptions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HealthCheckOptions>(builder =>
            {
                builder
                .HasOne(p => p.Passive)
                .WithOne(s => s.HealthCheckOptions)
                .OnDelete(DeleteBehavior.Cascade);
                builder
                .HasOne(p => p.Active)
                .WithOne(s => s.HealthCheckOptions)
                .OnDelete(DeleteBehavior.Cascade);
            });
        }
        private void BuildProxyRoute(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProxyRoute>(builder =>
            {
                builder
                .HasMany(p => p.Metadata)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
                builder
                .HasMany(p => p.Transforms)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
                builder
                .HasOne(p => p.Match)
                .WithOne(m => m.ProxyRoute)
                .OnDelete(DeleteBehavior.Cascade);
            });
        }
        private void BuildProxyMatch(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProxyMatch>(builder =>
            {
                builder
                .HasMany(p => p.Headers)
                .WithOne(h => h.ProxyMatch)
                .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
