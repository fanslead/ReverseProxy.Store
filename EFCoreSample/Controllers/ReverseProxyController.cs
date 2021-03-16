using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ReverseProxy.Store;
using ReverseProxy.Store.EFCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFCoreSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReverseProxyController : ControllerBase
    {
        private readonly ILogger<ReverseProxyController> _logger;
        private readonly EFCoreDbContext _dbContext;
        private readonly IReverseProxyStore _reverseProxyStore;
        public ReverseProxyController(ILogger<ReverseProxyController> logger, EFCoreDbContext dbContext, IReverseProxyStore reverseProxyStore)
        {
            _logger = logger;
            _dbContext = dbContext;
            _reverseProxyStore = reverseProxyStore;
        }
        [HttpGet("Cluster")]
        public async Task<ActionResult> GetCluster()
        {
            var clusters = await _dbContext.Set<Cluster>()
                   .Include(c => c.Metadata)
                   .Include(c => c.Destinations)
                   .Include(c => c.SessionAffinity).ThenInclude(s => s.Settings)
                   .Include(c => c.HttpRequest)
                   .Include(c => c.HttpClient)
                   .Include(c => c.HealthCheck).ThenInclude(h => h.Active)
                   .Include(c => c.HealthCheck).ThenInclude(h => h.Passive)
                   .AsNoTracking().ToListAsync();
            return Ok(clusters);
        }
        [HttpGet("ClusterPage")]
        public async Task<ActionResult> GetClusterPage(int pageIndex = 1, int pageSize = 10)
        {
            var clusters = await _dbContext.Set<Cluster>()
                   .Include(c => c.Metadata)
                   .Include(c => c.Destinations)
                   .Include(c => c.SessionAffinity).ThenInclude(s => s.Settings)
                   .Include(c => c.HttpRequest)
                   .Include(c => c.HttpClient)
                   .Include(c => c.HealthCheck).ThenInclude(h => h.Active)
                   .Include(c => c.HealthCheck).ThenInclude(h => h.Passive)
                   .Skip((pageIndex - 1) * pageSize)
                   .Take(pageSize)
                   .AsNoTracking()
                   .ToListAsync();
            var total = await _dbContext.Set<Cluster>().CountAsync();
            return Ok(new { Total = total, Data = clusters });
        }
        [HttpPost("Cluster")]
        public async Task<ActionResult> AddCluster(Cluster cluster)
        {
            await _dbContext.Set<Cluster>().AddAsync(cluster);
            await _dbContext.SaveChangesAsync();
            ReloadConfig();
            return Ok(new { Data = true });
        }
        [HttpPut("Cluster")]
        public async Task<ActionResult> UpdateCluster(Cluster cluster)
        {
            var dbCluster = await _dbContext.Set<Cluster>()
                   .Include(c => c.Metadata)
                   .Include(c => c.Destinations)
                   .Include(c => c.SessionAffinity).ThenInclude(s => s.Settings)
                   .Include(c => c.HttpRequest)
                   .Include(c => c.HttpClient)
                   .Include(c => c.HealthCheck).ThenInclude(h => h.Active)
                   .Include(c => c.HealthCheck).ThenInclude(h => h.Passive)
                   .FirstAsync(c=>c.Id == cluster.Id);
            using (var tran = _dbContext.Database.BeginTransaction())
            {
                try
                {

                    if (dbCluster.HealthCheck != null)
                        _dbContext.Remove(dbCluster.HealthCheck);
                    if (dbCluster.Destinations != null)
                        _dbContext.RemoveRange(dbCluster.Destinations);
                    if (dbCluster.HttpClient != null)
                        _dbContext.Remove(dbCluster.HttpClient);
                    if (dbCluster.HttpRequest != null)
                        _dbContext.Remove(dbCluster.HttpRequest);
                    if (dbCluster.SessionAffinity != null)
                        _dbContext.Remove(dbCluster.SessionAffinity);
                    if (dbCluster.Metadata != null)
                        _dbContext.RemoveRange(dbCluster.Metadata);

                    await _dbContext.SaveChangesAsync();

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

                    _dbContext.Update(dbCluster);
                    await _dbContext.SaveChangesAsync();
                    await tran.CommitAsync();
                    ReloadConfig();
                }catch(Exception ex)
                {
                    await tran.RollbackAsync();
                    return Ok(new { Data = false, Message = ex.Message });
                }
            }
            return Ok(new { Data = true });
        }
        [HttpDelete("Cluster")]
        public async Task<ActionResult> DeleteCluster(string clusterId)
        {
            var cluster = await _dbContext.Set<Cluster>().FirstOrDefaultAsync(c => c.Id == clusterId);
            if (cluster is null)
                return NotFound();
            _dbContext.Set<Cluster>().Remove(cluster);
            await _dbContext.SaveChangesAsync();
            ReloadConfig();
            return Ok(new { Data = true });
        }
        [HttpGet("ProxyRoute")]
        public async Task<ActionResult> GetProxyRoute()
        {
            var routers = await _dbContext.Set<ProxyRoute>()
                .Include(r => r.Match).ThenInclude(m => m.Headers)
                .Include(r => r.Metadata)
                .Include(r => r.Transforms)
                .AsNoTracking().ToListAsync();
            return Ok(routers);
        }
        [HttpGet("ProxyRoutePage")]
        public async Task<ActionResult> GetProxyRoutePage(int pageIndex = 1, int pageSize = 10)
        {
            var routers = await _dbContext.Set<ProxyRoute>()
                .Include(r => r.Match).ThenInclude(m => m.Headers)
                .Include(r => r.Metadata)
                .Include(r => r.Transforms)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
            var total = await _dbContext.Set<ProxyRoute>().CountAsync();
            return Ok(new { Total = total, Data = routers });
        }
        [HttpPost("ProxyRoute")]
        public async Task<ActionResult> AddProxyRoute(ProxyRoute proxyRoute)
        {
            await _dbContext.Set<ProxyRoute>().AddAsync(proxyRoute);
            await _dbContext.SaveChangesAsync();
            ReloadConfig();
            return Ok(new { Data = true });
        }
        [HttpPut("ProxyRoute")]
        public async Task<ActionResult> UpdateProxyRoute(ProxyRoute proxyRoute)
        {
            var dbRoute = await _dbContext.Set<ProxyRoute>()
                .Include(r => r.Match).ThenInclude(m => m.Headers)
                .Include(r => r.Metadata)
                .Include(r => r.Transforms)
                .FirstAsync(r => r.Id == proxyRoute.Id);
            using (var tran = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    if (dbRoute.Match != null)
                        _dbContext.Remove(dbRoute.Match);
                    if (dbRoute.Transforms != null)
                        _dbContext.RemoveRange(dbRoute.Transforms);
                    if (dbRoute.Metadata != null)
                        _dbContext.RemoveRange(dbRoute.Metadata);

                    await _dbContext.SaveChangesAsync();

                    if (proxyRoute.Match != null)
                    {
                        proxyRoute.Match.Id = 0;
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
                    _dbContext.Update(dbRoute);
                    await _dbContext.SaveChangesAsync();
                    await tran.CommitAsync();
                    ReloadConfig();
                }catch(Exception ex)
                {
                    await tran.RollbackAsync();
                    return Ok(new { Data = false, Message = ex.Message });
                }
            }
            return Ok(new { Data = true });
        }
        [HttpDelete("ProxyRoute")]
        public async Task<ActionResult> DeleteProxyRoute(int routeId)
        {
            var cluster = await _dbContext.Set<ProxyRoute>().FirstOrDefaultAsync(c => c.Id == routeId);
            if (cluster is null)
                return NotFound();
            _dbContext.Set<ProxyRoute>().Remove(cluster);
            await _dbContext.SaveChangesAsync();
            ReloadConfig();
            return Ok(new { Data = true });
        }

        private void ReloadConfig()
        {
            Task.Factory.StartNew(() => _reverseProxyStore.Reload());
        }
    }
}
