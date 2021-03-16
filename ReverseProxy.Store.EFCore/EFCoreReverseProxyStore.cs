using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.ReverseProxy.Service;
using Microsoft.ReverseProxy.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace ReverseProxy.Store.EFCore
{
    public class EFCoreReverseProxyStore : IReverseProxyStore
    {
        private EFCoreReloadToken _reloadToken = new EFCoreReloadToken();
        private IServiceProvider _sp;
        private IMemoryCache _cache;
        //private readonly ICertificateConfigLoader _certificateConfigLoader;

        public EFCoreReverseProxyStore(IServiceProvider sp, IMemoryCache cache)
        {
            _sp = sp;
            _cache = cache;
        }

        public IProxyConfig GetConfig()
        {
            var exist = _cache.TryGetValue<IProxyConfig>("ReverseProxyConfig", out IProxyConfig config);
            if (exist)
            {
                return config;
            }
            else
            {
                config = GetFromDb();
                SetConfig(config);

                return config;
            }
        }


        public IChangeToken GetReloadToken()
        {
            return _reloadToken;
        }

        public void Reload()
        {
            SetConfig();
            Interlocked.Exchange<EFCoreReloadToken>(ref this._reloadToken,
                new EFCoreReloadToken()).OnReload();
        }

        private void SetConfig()
        {
            var config = GetFromDb();
            SetConfig(config);
        }
        private void SetConfig(IProxyConfig config)
        {
            _cache.Set("ReverseProxyConfig", config);
        }
        private IProxyConfig GetFromDb()
        {
            var DbContext = _sp.CreateScope().ServiceProvider.GetRequiredService<EFCoreDbContext>();
            var routers = DbContext.Set<ProxyRoute>()
                .Include(r => r.Match).ThenInclude(m => m.Headers)
                .Include(r => r.Metadata)
                .Include(r => r.Transforms)
                .AsNoTracking().ToList();
            var clusters = DbContext.Set<Cluster>()
                .Include(c => c.Metadata)
                .Include(c => c.Destinations)
                .Include(c => c.SessionAffinity).ThenInclude(s => s.Settings)
                .Include(c => c.HttpRequest)
                .Include(c => c.HttpClient)
                .Include(c => c.HealthCheck).ThenInclude(h => h.Active)
                .Include(c => c.HealthCheck).ThenInclude(h => h.Passive)
                .AsNoTracking().ToList();
            var newConfig = new StoreProxyConfig();
            foreach (var section in clusters)
            {
                newConfig.Clusters.Add(CreateCluster(section));
            }

            foreach (var section in routers)
            {
                newConfig.Routes.Add(CreateRoute(section));
            }
            return newConfig;
        }


        private Microsoft.ReverseProxy.Abstractions.Cluster CreateCluster(Cluster cluster)
        {
            var destinations = new Dictionary<string, Microsoft.ReverseProxy.Abstractions.Destination>(StringComparer.OrdinalIgnoreCase);
            foreach (var destination in cluster.Destinations)
            {
                destinations.Add(destination.Name, CreateDestination(destination));
            }

            return new Microsoft.ReverseProxy.Abstractions.Cluster
            {
                Id = cluster.Id,
                LoadBalancingPolicy = cluster.LoadBalancingPolicy,
                SessionAffinity = CreateSessionAffinityOptions(cluster.SessionAffinity),
                HealthCheck = CreateHealthCheckOptions(cluster.HealthCheck),
                HttpClient = CreateProxyHttpClientOptions(cluster.HttpClient),
                HttpRequest = CreateProxyRequestOptions(cluster.HttpRequest),
                Metadata = cluster.Metadata.ReadStringDictionary(),
                Destinations = destinations,
            };
        }

        private static Microsoft.ReverseProxy.Abstractions.ProxyRoute CreateRoute(ProxyRoute proxyRoute)
        {
            return new Microsoft.ReverseProxy.Abstractions.ProxyRoute
            {
                RouteId = proxyRoute.RouteId,
                Order = proxyRoute.Order,
                ClusterId = proxyRoute.ClusterId,
                AuthorizationPolicy = proxyRoute.AuthorizationPolicy,
                CorsPolicy = proxyRoute.CorsPolicy,
                Metadata = proxyRoute.Metadata.ReadStringDictionary(),
                Transforms = CreateTransforms(proxyRoute.Transforms),
                Match = CreateProxyMatch(proxyRoute.Match),
            };
        }

        private static IReadOnlyList<IReadOnlyDictionary<string, string>> CreateTransforms(List<Transform> transforms)
        {
            if (transforms is null || transforms.Count == 0)
            {
                return null;
            }
            var groupTransforms = transforms.GroupBy(t => t.Type);
            var list = new List<IReadOnlyDictionary<string, string>>();
            foreach (var item in groupTransforms)
            {
                list.Add(item.ToDictionary(d => d.Key, d => d.Value, StringComparer.OrdinalIgnoreCase));
            }
            return list;
        }

        private static Microsoft.ReverseProxy.Abstractions.ProxyMatch CreateProxyMatch(ProxyMatch match)
        {
            if (match is null)
            {
                return null;
            }

            return new Microsoft.ReverseProxy.Abstractions.ProxyMatch()
            {
                Methods = match.Methods.ReadStringArray(),
                Hosts = match.Hosts.ReadStringArray(),
                Path = match.Path,
                Headers = CreateRouteHeaders(match.Headers),
            };
        }

        private static IReadOnlyList<Microsoft.ReverseProxy.Abstractions.RouteHeader> CreateRouteHeaders(List<RouteHeader> routeHeaders)
        {
            if (routeHeaders is null || routeHeaders.Count == 0)
            {
                return null;
            }

            return routeHeaders.Select(data => CreateRouteHeader(data)).ToArray();
        }

        private static Microsoft.ReverseProxy.Abstractions.RouteHeader CreateRouteHeader(RouteHeader routeHeader)
        {
            return new Microsoft.ReverseProxy.Abstractions.RouteHeader()
            {
                Name = routeHeader.Name,
                Values = routeHeader.Values.ReadStringArray(),
                Mode = routeHeader.Mode,
                IsCaseSensitive = routeHeader.IsCaseSensitive,
            };
        }

        private static Microsoft.ReverseProxy.Abstractions.SessionAffinityOptions CreateSessionAffinityOptions(SessionAffinityOptions sessionAffinityOptions)
        {
            if (sessionAffinityOptions is null)
            {
                return null;
            }

            return new Microsoft.ReverseProxy.Abstractions.SessionAffinityOptions
            {
                Enabled = sessionAffinityOptions.Enabled ?? false,
                Mode = sessionAffinityOptions.Mode,
                FailurePolicy = sessionAffinityOptions.FailurePolicy,
                Settings = sessionAffinityOptions.Settings.ReadStringDictionary()
            };
        }

        private static Microsoft.ReverseProxy.Abstractions.HealthCheckOptions CreateHealthCheckOptions(HealthCheckOptions healthCheckOptions)
        {
            if (healthCheckOptions is null)
            {
                return null;
            }

            return new Microsoft.ReverseProxy.Abstractions.HealthCheckOptions
            {
                Passive = CreatePassiveHealthCheckOptions(healthCheckOptions.Passive),
                Active = CreateActiveHealthCheckOptions(healthCheckOptions.Active)
            };
        }

        private static Microsoft.ReverseProxy.Abstractions.PassiveHealthCheckOptions CreatePassiveHealthCheckOptions(PassiveHealthCheckOptions passiveHealthCheckOptions)
        {
            if (passiveHealthCheckOptions is null)
            {
                return null;
            }

            return new Microsoft.ReverseProxy.Abstractions.PassiveHealthCheckOptions
            {
                Enabled = passiveHealthCheckOptions.Enabled ?? false,
                Policy = passiveHealthCheckOptions.Policy,
                ReactivationPeriod = passiveHealthCheckOptions.ReactivationPeriod.ReadTimeSpan()
            };
        }

        private static Microsoft.ReverseProxy.Abstractions.ActiveHealthCheckOptions CreateActiveHealthCheckOptions(ActiveHealthCheckOptions activeHealthCheckOptions)
        {
            if (activeHealthCheckOptions is null)
            {
                return null;
            }

            return new Microsoft.ReverseProxy.Abstractions.ActiveHealthCheckOptions
            {
                Enabled = activeHealthCheckOptions.Enabled ?? false,
                Interval = activeHealthCheckOptions.Interval.ReadTimeSpan(),
                Timeout = activeHealthCheckOptions.Timeout.ReadTimeSpan(),
                Policy = activeHealthCheckOptions.Policy,
                Path = activeHealthCheckOptions.Path
            };
        }

        private Microsoft.ReverseProxy.Abstractions.ProxyHttpClientOptions CreateProxyHttpClientOptions(ProxyHttpClientOptions proxyHttpClientOptions)
        {
            if (proxyHttpClientOptions is null)
            {
                return null;
            }

            //var certSection = proxyHttpClientOptions.ClientCertificate;

            X509Certificate2 clientCertificate = null;

            //if (certSection.Exists())
            //{
            //    clientCertificate = _certificateConfigLoader.LoadCertificate(certSection);
            //}

            //if (clientCertificate != null)
            //{
            //    Certificates.AddLast(new WeakReference<X509Certificate2>(clientCertificate));
            //}

            SslProtocols? sslProtocols = null;
            if (!string.IsNullOrWhiteSpace(proxyHttpClientOptions?.SslProtocols))
            {

                foreach (var protocolConfig in proxyHttpClientOptions?.SslProtocols?.Split(",").Select(s => Enum.Parse<SslProtocols>(s, ignoreCase: true)))
                {
                    sslProtocols = sslProtocols == null ? protocolConfig : sslProtocols | protocolConfig;
                }
            }
            else
            {
                sslProtocols = SslProtocols.None;
            }

            return new Microsoft.ReverseProxy.Abstractions.ProxyHttpClientOptions
            {
                SslProtocols = sslProtocols,
                DangerousAcceptAnyServerCertificate = proxyHttpClientOptions.DangerousAcceptAnyServerCertificate,
                ClientCertificate = clientCertificate,
                MaxConnectionsPerServer = proxyHttpClientOptions.MaxConnectionsPerServer,
#if NET
                EnableMultipleHttp2Connections = proxyHttpClientOptions.EnableMultipleHttp2Connections,
#endif
                ActivityContextHeaders = proxyHttpClientOptions.ActivityContextHeaders.ReadEnum<Microsoft.ReverseProxy.Abstractions.ActivityContextHeaders>()
            };
        }

        private static Microsoft.ReverseProxy.Service.Proxy.RequestProxyOptions CreateProxyRequestOptions(RequestProxyOptions requestProxyOptions)
        {
            if (requestProxyOptions is null)
            {
                return null;
            }

            return new Microsoft.ReverseProxy.Service.Proxy.RequestProxyOptions
            {
                Timeout = requestProxyOptions.Timeout.ReadTimeSpan(),
                Version = requestProxyOptions.Version.ReadVersion(),
#if NET
                VersionPolicy = requestProxyOptions.VersionPolicy.ReadEnum<HttpVersionPolicy>(),
#endif
            };
        }

        private static Microsoft.ReverseProxy.Abstractions.Destination CreateDestination(Destination destination)
        {
            if (destination is null)
            {
                return null;
            }

            return new Microsoft.ReverseProxy.Abstractions.Destination
            {
                Address = destination.Address,
                Health = destination.Health,
                Metadata = destination.Metadata.ReadStringDictionary(),
            };
        }
    }
}
