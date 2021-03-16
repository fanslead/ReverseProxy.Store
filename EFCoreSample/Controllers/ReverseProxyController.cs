using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ReverseProxy.Store;
using ReverseProxy.Store.EFCore;
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
            if (cluster.HealthCheck != null)
                _dbContext.Remove(cluster.HealthCheck);
            if (cluster.Destinations != null)
                _dbContext.RemoveRange(cluster.Destinations);
            if (cluster.HttpClient != null)
                _dbContext.Remove(cluster.HttpClient);
            if (cluster.HttpRequest != null)
                _dbContext.Remove(cluster.HttpRequest);
            if (cluster.SessionAffinity != null)
                _dbContext.Remove(cluster.SessionAffinity);
            if (cluster.Metadata != null)
                _dbContext.RemoveRange(cluster.Metadata);

            await _dbContext.SaveChangesAsync();

            if (cluster.HealthCheck != null)
                cluster.HealthCheck.Id = 0;
            if (cluster.Destinations != null)
                cluster.Destinations.ForEach(d => d.Id = 0);
            if (cluster.HttpClient != null)
                cluster.HttpClient.Id = 0;
            if (cluster.HttpRequest != null)
                cluster.HttpRequest.Id = 0;
            if (cluster.SessionAffinity != null)
                cluster.SessionAffinity.Id = 0;
            if (cluster.Metadata != null)
                cluster.Metadata.ForEach(d => d.Id = 0);

            _dbContext.Update(cluster);
            await _dbContext.SaveChangesAsync();
            ReloadConfig();
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
            if (proxyRoute.Match != null)
                _dbContext.Remove(proxyRoute.Match);
            if (proxyRoute.Transforms != null)
                _dbContext.RemoveRange(proxyRoute.Transforms);
            if (proxyRoute.Metadata != null)
                _dbContext.RemoveRange(proxyRoute.Metadata);

            await _dbContext.SaveChangesAsync();

            if (proxyRoute.Match != null)
            {
                proxyRoute.Match.Id = 0;
                proxyRoute.Match.Headers.ForEach(d => d.Id = 0);
            }
            if (proxyRoute.Transforms != null)
                proxyRoute.Transforms.ForEach(d => d.Id = 0);
            if (proxyRoute.Metadata != null)
                proxyRoute.Metadata.ForEach(d => d.Id = 0);
            _dbContext.Update(proxyRoute);
            await _dbContext.SaveChangesAsync();
            ReloadConfig();
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
