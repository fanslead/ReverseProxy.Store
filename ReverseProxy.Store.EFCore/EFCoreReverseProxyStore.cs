namespace ReverseProxy.Store.EFCore;

public class EFCoreReverseProxyStore : IReverseProxyStore
{
    private EFCoreReloadToken _reloadToken = new EFCoreReloadToken();
    private IServiceProvider _sp;
    private IMemoryCache _cache;
    private ILogger Logger;
    public event ConfigChangeHandler ChangeConfig;

    public EFCoreReverseProxyStore(IServiceProvider sp, IMemoryCache cache, ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<EFCoreReverseProxyStore>();
        _sp = sp;
        _cache = cache;
        ChangeConfig += ReloadConfig;
    }

    // Used by tests
    internal LinkedList<WeakReference<X509Certificate2>> Certificates { get; } = new LinkedList<WeakReference<X509Certificate2>>();

    public IProxyConfig GetConfig()
    {
        Logger.LogInformation("GetConfig");
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
        Logger.LogInformation("ChangeConfig");
        if (ChangeConfig != null)
            ChangeConfig();
    }
    public void ReloadConfig()
    {
        Logger.LogInformation("SetConfig");
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
            .Include(c => c.SessionAffinity).ThenInclude(s => s.Cookie)
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


    private Yarp.ReverseProxy.Configuration.ClusterConfig CreateCluster(Cluster cluster)
    {
        var destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var destination in cluster.Destinations)
        {
            destinations.Add(destination.Name, CreateDestination(destination));
        }

        return new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = cluster.Id,
            LoadBalancingPolicy = cluster.LoadBalancingPolicy,
            SessionAffinity = CreateSessionAffinityOptions(cluster.SessionAffinity),
            HealthCheck = CreateHealthCheckOptions(cluster.HealthCheck),
            HttpClient = CreateHttpClientConfig(cluster.HttpClient),
            HttpRequest = CreateProxyRequestConfig(cluster.HttpRequest),
            Metadata = cluster.Metadata.ReadStringDictionary(),
            Destinations = destinations,
        };
    }

    private static Yarp.ReverseProxy.Configuration.RouteConfig CreateRoute(ProxyRoute proxyRoute)
    {
        if (string.IsNullOrEmpty(proxyRoute.RouteId))
        {
            throw new Exception("The route config format has changed, routes are now objects instead of an array. The route id must be set as the object name, not with the 'RouteId' field.");
        }
        return new Yarp.ReverseProxy.Configuration.RouteConfig
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
        var groupTransforms = transforms.OrderBy(t => t.Id).GroupBy(t => t.Type);
        var list = new List<IReadOnlyDictionary<string, string>>();
        foreach (var group in groupTransforms)
        {
            var key = group.Key.ToString();
            Dictionary<string, string> dir = new Dictionary<string, string>();
            foreach (var transform in group)
            {
                if (transform.Key == key)
                {
                    if (dir.Count != 0)
                        list.Add(dir);
                    dir = new Dictionary<string, string>();
                }
                dir.Add(transform.Key, transform.Value);
            }
            if (dir.Count != 0)
                list.Add(dir);
        }
        return list;
    }

    private static Yarp.ReverseProxy.Configuration.RouteMatch CreateProxyMatch(ProxyMatch match)
    {
        if (match is null)
        {
            return null;
        }

        return new Yarp.ReverseProxy.Configuration.RouteMatch()
        {
            Methods = match.Methods.ReadStringArray(),
            Hosts = match.Hosts.ReadStringArray(),
            Path = match.Path,
            Headers = CreateRouteHeaders(match.Headers),
            QueryParameters = CreateRouteQueryParameters(match.QueryParameters)
        };
    }

    private static IReadOnlyList<Yarp.ReverseProxy.Configuration.RouteHeader>? CreateRouteHeaders(List<RouteHeader> routeHeaders)
    {
        if (routeHeaders is null || routeHeaders.Count == 0)
        {
            return null;
        }

        return routeHeaders.Select(data => CreateRouteHeader(data)).ToArray();
    }

    private static Yarp.ReverseProxy.Configuration.RouteHeader CreateRouteHeader(RouteHeader routeHeader)
    {
        return new Yarp.ReverseProxy.Configuration.RouteHeader()
        {
            Name = routeHeader.Name,
            Values = routeHeader.Values.ReadStringArray(),
            Mode = routeHeader.Mode,
            IsCaseSensitive = routeHeader.IsCaseSensitive,
        };
    }

    private static IReadOnlyList<RouteQueryParameter>? CreateRouteQueryParameters(IReadOnlyList<Entities.RouteQueryParameter> routeQueryParameters)
    {
        if (routeQueryParameters is null)
        {
            return null;
        }

        return routeQueryParameters.Select(data => CreateRouteQueryParameter(data)).ToArray();
    }

    private static RouteQueryParameter CreateRouteQueryParameter(Entities.RouteQueryParameter routeQueryParameter)
    {
        return new RouteQueryParameter()
        {
            Name = routeQueryParameter.Name,
            Values = routeQueryParameter.Values.ReadStringArray(),
            Mode = routeQueryParameter.Mode,
            IsCaseSensitive = routeQueryParameter.IsCaseSensitive,
        };
    }
    private static Yarp.ReverseProxy.Configuration.SessionAffinityConfig? CreateSessionAffinityOptions(SessionAffinityConfig sessionAffinityOptions)
    {
        if (sessionAffinityOptions is null)
        {
            return null;
        }

        return new Yarp.ReverseProxy.Configuration.SessionAffinityConfig
        {
            Policy = sessionAffinityOptions.Policy,
            FailurePolicy = sessionAffinityOptions.FailurePolicy,
            AffinityKeyName = sessionAffinityOptions.AffinityKeyName,
            Enabled = sessionAffinityOptions.Enabled ?? false,
            Cookie = CreateSessionAffinityCookieConfig(sessionAffinityOptions.Cookie)
        };
    }

    private static Yarp.ReverseProxy.Configuration.SessionAffinityCookieConfig? CreateSessionAffinityCookieConfig(Entities.SessionAffinityCookie sessionAffinityCookie)
    {
        if (sessionAffinityCookie is null)
        {
            return null;
        }

        return new SessionAffinityCookieConfig
        {
            Path = sessionAffinityCookie.Path,
            SameSite = sessionAffinityCookie.SameSite,
            HttpOnly = sessionAffinityCookie.HttpOnly,
            MaxAge = sessionAffinityCookie.MaxAge.ReadTimeSpan(),
            Domain = sessionAffinityCookie.Domain,
            IsEssential = sessionAffinityCookie.IsEssential,
            SecurePolicy = sessionAffinityCookie.SecurePolicy,
            Expiration = sessionAffinityCookie.Expiration.ReadTimeSpan()
        };
    }
    private static Yarp.ReverseProxy.Configuration.HealthCheckConfig? CreateHealthCheckOptions(HealthCheckOptions healthCheckOptions)
    {
        if (healthCheckOptions is null)
        {
            return null;
        }

        return new Yarp.ReverseProxy.Configuration.HealthCheckConfig
        {
            Passive = CreatePassiveHealthCheckOptions(healthCheckOptions.Passive),
            Active = CreateActiveHealthCheckOptions(healthCheckOptions.Active),
            AvailableDestinationsPolicy = healthCheckOptions.AvailableDestinationsPolicy
        };
    }

    private static Yarp.ReverseProxy.Configuration.PassiveHealthCheckConfig? CreatePassiveHealthCheckOptions(PassiveHealthCheckOptions passiveHealthCheckOptions)
    {
        if (passiveHealthCheckOptions is null)
        {
            return null;
        }

        return new Yarp.ReverseProxy.Configuration.PassiveHealthCheckConfig
        {
            Enabled = passiveHealthCheckOptions.Enabled,
            Policy = passiveHealthCheckOptions.Policy,
            ReactivationPeriod = passiveHealthCheckOptions.ReactivationPeriod.ReadTimeSpan()
        };
    }

    private static Yarp.ReverseProxy.Configuration.ActiveHealthCheckConfig? CreateActiveHealthCheckOptions(ActiveHealthCheckOptions activeHealthCheckOptions)
    {
        if (activeHealthCheckOptions is null)
        {
            return null;
        }

        return new Yarp.ReverseProxy.Configuration.ActiveHealthCheckConfig
        {
            Enabled = activeHealthCheckOptions.Enabled,
            Interval = activeHealthCheckOptions.Interval.ReadTimeSpan(),
            Timeout = activeHealthCheckOptions.Timeout.ReadTimeSpan(),
            Policy = activeHealthCheckOptions.Policy,
            Path = activeHealthCheckOptions.Path
        };
    }

    private static Yarp.ReverseProxy.Configuration.HttpClientConfig? CreateHttpClientConfig(HttpClientConfig proxyHttpClientOptions)
    {
        if (proxyHttpClientOptions is null)
        {
            return null;
        }

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

        Yarp.ReverseProxy.Configuration.WebProxyConfig? webProxy;
        var webProxySection = proxyHttpClientOptions.WebProxy;
        if (webProxySection != null)
        {
            webProxy = new Yarp.ReverseProxy.Configuration.WebProxyConfig()
            {
                Address = string.IsNullOrWhiteSpace(webProxySection.Address) ? null : new Uri(webProxySection.Address),
                BypassOnLocal = webProxySection.BypassOnLocal,
                UseDefaultCredentials = webProxySection.UseDefaultCredentials
            };
        }
        else
        {
            webProxy = null;
        }
        return new Yarp.ReverseProxy.Configuration.HttpClientConfig
        {
            SslProtocols = sslProtocols,
            DangerousAcceptAnyServerCertificate = proxyHttpClientOptions.DangerousAcceptAnyServerCertificate,
            MaxConnectionsPerServer = proxyHttpClientOptions.MaxConnectionsPerServer,
#if NET
            EnableMultipleHttp2Connections = proxyHttpClientOptions.EnableMultipleHttp2Connections,
            RequestHeaderEncoding = proxyHttpClientOptions.RequestHeaderEncoding,
#endif
            WebProxy = webProxy
        };
    }

    private static Yarp.ReverseProxy.Forwarder.ForwarderRequestConfig? CreateProxyRequestConfig(ForwarderRequest requestProxyOptions)
    {
        if (requestProxyOptions is null)
        {
            return null;
        }

        return new Yarp.ReverseProxy.Forwarder.ForwarderRequestConfig
        {
            ActivityTimeout = requestProxyOptions.ActivityTimeout.ReadTimeSpan(),
            Version = requestProxyOptions.Version.ReadVersion(),
#if NET
            VersionPolicy = requestProxyOptions.VersionPolicy.ReadEnum<HttpVersionPolicy>(),
#endif
            AllowResponseBuffering = requestProxyOptions.AllowResponseBuffering
        };
    }

    private static Yarp.ReverseProxy.Configuration.DestinationConfig CreateDestination(Destination destination)
    {
        if (destination is null)
        {
            return null;
        }

        return new Yarp.ReverseProxy.Configuration.DestinationConfig
        {
            Address = destination.Address,
            Health = destination.Health,
            Metadata = destination.Metadata.ReadStringDictionary(),
        };
    }

}
