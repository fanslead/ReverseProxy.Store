namespace ReverseProxy.Store.EFCore.Management;

public class ProxyRouteManagement : IProxyRouteManagement
{
    private readonly ILogger<ProxyRouteManagement> _logger;
    private EFCoreDbContext DbContext;
    private readonly IReverseProxyStore _reverseProxyStore;

    public ProxyRouteManagement(EFCoreDbContext dbContext, IReverseProxyStore reverseProxyStore, ILogger<ProxyRouteManagement> logger)
    {
        DbContext = dbContext;
        _reverseProxyStore = reverseProxyStore;
        _logger = logger;
    }

    public async Task<bool> Create(ProxyRoute proxyRoute)
    {
        await DbContext.Set<ProxyRoute>().AddAsync(proxyRoute);
        var res = await DbContext.SaveChangesAsync();
        if (res > 0)
        {
            _logger.LogInformation("Create Cluster Success.");
            ReloadConfig();
            return true;
        }
        return false;
    }

    public async Task<bool> Delete(int id)
    {
        var proxyRoute = await DbContext.Set<ProxyRoute>().FirstOrDefaultAsync(c => c.Id == id);

        if (proxyRoute is null)
        {
            _logger.LogError("ProxyRoute Not Exist");
            return false;
        }
        DbContext.Set<ProxyRoute>().Remove(proxyRoute);
        await DbContext.SaveChangesAsync();
        ReloadConfig();
        _logger.LogInformation($"Delete ProxyRoute: {proxyRoute.RouteId} Success.");
        return true;
    }

    public async Task<ProxyRoute> Find(int id)
    {
        return await DbContext.Set<ProxyRoute>().FirstOrDefaultAsync(c => c.Id == id);
    }

    public IQueryable<ProxyRoute> GetAll()
    {
        return DbContext.Set<ProxyRoute>().AsNoTracking();
    }

    public async Task<bool> Update(ProxyRoute proxyRoute)
    {
        var dbRoute = await DbContext.Set<ProxyRoute>()
               .Include(r => r.Match).ThenInclude(m => m.Headers)
               .Include(r => r.Metadata)
               .Include(r => r.Transforms)
               .FirstAsync(r => r.Id == proxyRoute.Id);
        using (var tran = DbContext.Database.BeginTransaction())
        {
            try
            {
                if (dbRoute.Match != null)
                    DbContext.Remove(dbRoute.Match);
                if (dbRoute.Transforms != null)
                    DbContext.RemoveRange(dbRoute.Transforms);
                if (dbRoute.Metadata != null)
                    DbContext.RemoveRange(dbRoute.Metadata);

                await DbContext.SaveChangesAsync();

                if (proxyRoute.Match != null)
                {
                    proxyRoute.Match.Id = 0;
                    if (proxyRoute.Match.Headers != null)
                        proxyRoute.Match.Headers.ForEach(d => d.Id = 0);
                    dbRoute.Match = proxyRoute.Match;
                }
                if (proxyRoute.Transforms != null)
                {
                    proxyRoute.Transforms.ForEach(d => d.Id = 0);
                    dbRoute.Transforms = proxyRoute.Transforms;
                }
                if (proxyRoute.Metadata != null)
                {
                    proxyRoute.Metadata.ForEach(d => d.Id = 0);
                    dbRoute.Metadata = proxyRoute.Metadata;
                }
                dbRoute.RouteId = proxyRoute.RouteId;
                dbRoute.ClusterId = proxyRoute.ClusterId;
                dbRoute.Order = proxyRoute.Order;
                dbRoute.AuthorizationPolicy = proxyRoute.AuthorizationPolicy;
                dbRoute.CorsPolicy = proxyRoute.CorsPolicy;
                DbContext.Update(dbRoute);
                await DbContext.SaveChangesAsync();
                await tran.CommitAsync();
                _logger.LogInformation($"Update ProxyRoute: {proxyRoute.RouteId} Success.");
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
