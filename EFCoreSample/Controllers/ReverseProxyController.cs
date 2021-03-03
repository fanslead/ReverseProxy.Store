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
            _reverseProxyStore.Reload();
            return Ok();
        }
        [HttpPut("Cluster")]
        public async Task<ActionResult> UpdateCluster(Cluster cluster)
        {
            _dbContext.Set<Cluster>().Update(cluster);
            await _dbContext.SaveChangesAsync();
            _reverseProxyStore.Reload();
            return Ok();
        }
        [HttpDelete("Cluster")]
        public async Task<ActionResult> DeleteCluster(string clusterId)
        {
            var cluster = await _dbContext.Set<Cluster>().FirstOrDefaultAsync(c => c.Id == clusterId);
            if (cluster is null)
                return NotFound();
            _dbContext.Set<Cluster>().Remove(cluster);
            await _dbContext.SaveChangesAsync();
            _reverseProxyStore.Reload();
            return Ok();
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
            _reverseProxyStore.Reload();
            return Ok();
        }
        [HttpPut("ProxyRoute")]
        public async Task<ActionResult> UpdateProxyRoute(ProxyRoute proxyRoute)
        {
            _dbContext.Set<ProxyRoute>().Update(proxyRoute);
            await _dbContext.SaveChangesAsync();
            _reverseProxyStore.Reload();
            return Ok();
        }
        [HttpDelete("ProxyRoute")]
        public async Task<ActionResult> DeleteProxyRoute(int routeId)
        {
            var cluster = await _dbContext.Set<ProxyRoute>().FirstOrDefaultAsync(c => c.Id == routeId);
            if (cluster is null)
                return NotFound();
            _dbContext.Set<ProxyRoute>().Remove(cluster);
            await _dbContext.SaveChangesAsync();
            _reverseProxyStore.Reload();
            return Ok();
        }
    }
}
