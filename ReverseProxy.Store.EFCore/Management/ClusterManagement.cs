namespace ReverseProxy.Store.EFCore.Management;

public class ClusterManagement : IClusterManagement
{
    private readonly ILogger<ClusterManagement> _logger;
    private EFCoreDbContext DbContext;
    private readonly IReverseProxyStore _reverseProxyStore;

    public ClusterManagement(EFCoreDbContext dbContext, IReverseProxyStore reverseProxyStore, ILogger<ClusterManagement> logger)
    {
        DbContext = dbContext;
        _reverseProxyStore = reverseProxyStore;
        _logger = logger;
    }

    public async Task<bool> Create(Cluster cluster)
    {
        await DbContext.Set<Cluster>().AddAsync(cluster);
        var res = await DbContext.SaveChangesAsync();
        if (res > 0)
        {
            _logger.LogInformation("Create Cluster Success.");
            ReloadConfig();
            return true;
        }
        return false;
    }

    public async Task<bool> Delete(string id)
    {
        var cluster = await DbContext.Set<Cluster>().FirstOrDefaultAsync(c => c.Id == id);
        if (cluster is null)
        {
            _logger.LogError("Cluster Not Exist");
            return false;
        }
        var useCount = await DbContext.Set<ProxyRoute>().CountAsync(p => p.ClusterId == id);
        if (useCount > 0)
        {
            _logger.LogError($"Cluster: {id} have been use.");
            return false;
        }
        var des = await DbContext.Set<Destination>().Include(d => d.Metadata).Where(d => d.ClusterId == cluster.Id).ToListAsync();
        foreach (var d in des)
        {
            DbContext.Set<Metadata>().RemoveRange(d.Metadata);
        }
        DbContext.Set<Cluster>().Remove(cluster);
        var res = await DbContext.SaveChangesAsync();
        if (res > 0)
        {
            _logger.LogInformation($"Delete Cluster: {id} Success.");
            ReloadConfig();
            return true;
        }
        return false;
    }

    public async Task<Cluster> Find(string id)
    {
        return await DbContext.Set<Cluster>().FirstOrDefaultAsync(c => c.Id == id);
    }

    public IQueryable<Cluster> GetAll()
    {
        return DbContext.Set<Cluster>().AsNoTracking();
    }

    public async Task<bool> Update(Cluster cluster)
    {
        var dbCluster = await DbContext.Set<Cluster>()
               .Include(c => c.Metadata)
               .Include(c => c.Destinations)
               .Include(c => c.SessionAffinity).ThenInclude(s => s.Cookie)
               .Include(c => c.HttpRequest)
               .Include(c => c.HttpClient)
               .Include(c => c.HealthCheck).ThenInclude(h => h.Active)
               .Include(c => c.HealthCheck).ThenInclude(h => h.Passive)
               .FirstAsync(c => c.Id == cluster.Id);
        using (var tran = DbContext.Database.BeginTransaction())
        {
            try
            {

                if (dbCluster.HealthCheck != null)
                    DbContext.Remove(dbCluster.HealthCheck);
                if (dbCluster.Destinations != null)
                    DbContext.RemoveRange(dbCluster.Destinations);
                if (dbCluster.HttpClient != null)
                    DbContext.Remove(dbCluster.HttpClient);
                if (dbCluster.HttpRequest != null)
                    DbContext.Remove(dbCluster.HttpRequest);
                if (dbCluster.SessionAffinity != null)
                    DbContext.Remove(dbCluster.SessionAffinity);
                if (dbCluster.Metadata != null)
                    DbContext.RemoveRange(dbCluster.Metadata);

                await DbContext.SaveChangesAsync();

                if (cluster.HealthCheck != null)
                {
                    cluster.HealthCheck.Id = 0;
                    dbCluster.HealthCheck = cluster.HealthCheck;
                }
                if (cluster.Destinations != null)
                {
                    cluster.Destinations.ForEach(d => d.Id = 0);
                    dbCluster.Destinations = cluster.Destinations;
                }
                if (cluster.HttpClient != null)
                {
                    cluster.HttpClient.Id = 0;
                    dbCluster.HttpClient = cluster.HttpClient;
                }
                if (cluster.HttpRequest != null)
                {
                    cluster.HttpRequest.Id = 0;
                    dbCluster.HttpRequest = cluster.HttpRequest;
                }
                if (cluster.SessionAffinity != null)
                {
                    cluster.SessionAffinity.Id = 0;
                    dbCluster.SessionAffinity = cluster.SessionAffinity;
                }
                if (cluster.Metadata != null)
                {
                    cluster.Metadata.ForEach(d => d.Id = 0);
                    dbCluster.Metadata = cluster.Metadata;
                }
                dbCluster.LoadBalancingPolicy = cluster.LoadBalancingPolicy;
                DbContext.Update(dbCluster);
                await DbContext.SaveChangesAsync();
                await tran.CommitAsync();
                _logger.LogInformation($"Update Cluster: {cluster.Id} Success.");
                ReloadConfig();
                return true;
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }
    }
    private void ReloadConfig()
    {
        Task.Factory.StartNew(() => _reverseProxyStore.Reload());
    }
}
