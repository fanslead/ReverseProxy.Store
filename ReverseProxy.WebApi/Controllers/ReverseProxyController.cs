using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReverseProxy.Store.EFCore.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReverseProxy.Store.Entities;
using Microsoft.Extensions.Configuration;

namespace ReverseProxy.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReverseProxyController : ControllerBase
    {
        private readonly ILogger<ReverseProxyController> _logger;
        private readonly IClusterManagement _clusterManagement;
        private readonly IProxyRouteManagement _proxyRouteManagement;
        private readonly IConfiguration _configuration;
        public ReverseProxyController(ILogger<ReverseProxyController> logger, IClusterManagement clusterManagement, IProxyRouteManagement proxyRouteManagement, IConfiguration configuration)
        {
            _logger = logger;
            _clusterManagement = clusterManagement;
            _proxyRouteManagement = proxyRouteManagement;
            _configuration = configuration;
        }
        [HttpPost("Login")]
        public async Task<ActionResult> Login(string password)
        {
            if (_configuration["Password"].Equals(password))
            {
                return Ok(new { Data = true });
            }
            return Ok(new { Data = false });
        }
        [HttpGet("Cluster")]
        public async Task<ActionResult> GetCluster()
        {
            var clusters = await _clusterManagement.GetAll()
                   .Include(c => c.Metadata)
                   .Include(c => c.Destinations)
                   .Include(c => c.SessionAffinity).ThenInclude(s => s.Cookie)
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
            var clusters = await _clusterManagement.GetAll()
                   .Include(c => c.Metadata)
                   .Include(c => c.Destinations)
                   .Include(c => c.SessionAffinity).ThenInclude(s => s.Cookie)
                   .Include(c => c.HttpRequest)
                   .Include(c => c.HttpClient)
                   .Include(c => c.HealthCheck).ThenInclude(h => h.Active)
                   .Include(c => c.HealthCheck).ThenInclude(h => h.Passive)
                   .Skip((pageIndex - 1) * pageSize)
                   .Take(pageSize)
                   .AsNoTracking()
                   .ToListAsync();
            var total = await _clusterManagement.GetAll().CountAsync();
            return Ok(new { Total = total, Data = clusters });
        }
        [HttpPost("Cluster")]
        public async Task<ActionResult> AddCluster(Cluster cluster)
        {
            var res = await _clusterManagement.Create(cluster);
            if (res)
                return Ok(new { Data = true });
            else
                return Ok(new { Data = false });
        }
        [HttpPut("Cluster")]
        public async Task<ActionResult> UpdateCluster(Cluster cluster)
        {
            var res = await _clusterManagement.Update(cluster);
            if (res)
                return Ok(new { Data = true });
            else
                return Ok(new { Data = false });
        }
        [HttpDelete("Cluster")]
        public async Task<ActionResult> DeleteCluster(string clusterId)
        {
            var res = await _clusterManagement.Delete(clusterId);
            if (res)
                return Ok(new { Data = true });
            else
                return Ok(new { Data = false });
        }
        [HttpGet("ProxyRoute")]
        public async Task<ActionResult> GetProxyRoute()
        {
            var routers = await _proxyRouteManagement.GetAll()
                .Include(r => r.Match).ThenInclude(m => m.Headers)
                .Include(r => r.Metadata)
                .Include(r => r.Transforms)
                .AsNoTracking().ToListAsync();
            return Ok(routers);
        }
        [HttpGet("ProxyRoutePage")]
        public async Task<ActionResult> GetProxyRoutePage(int pageIndex = 1, int pageSize = 10)
        {
            var routers = await _proxyRouteManagement.GetAll()
                .Include(r => r.Match).ThenInclude(m => m.Headers)
                .Include(r => r.Metadata)
                .Include(r => r.Transforms)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
            var total = await _proxyRouteManagement.GetAll().CountAsync();
            return Ok(new { Total = total, Data = routers });
        }
        [HttpPost("ProxyRoute")]
        public async Task<ActionResult> AddProxyRoute(ProxyRoute proxyRoute)
        {
            var res = await _proxyRouteManagement.Create(proxyRoute);
            if (res)
                return Ok(new { Data = true });
            else
                return Ok(new { Data = false });
        }
        [HttpPut("ProxyRoute")]
        public async Task<ActionResult> UpdateProxyRoute(ProxyRoute proxyRoute)
        {
            var res = await _proxyRouteManagement.Update(proxyRoute);
            if (res)
                return Ok(new { Data = true });
            else
                return Ok(new { Data = false });
        }
        [HttpDelete("ProxyRoute")]
        public async Task<ActionResult> DeleteProxyRoute(int routeId)
        {
            var res = await _proxyRouteManagement.Delete(routeId);
            if (res)
                return Ok(new { Data = true });
            else
                return Ok(new { Data = false });
        }
    }
}
