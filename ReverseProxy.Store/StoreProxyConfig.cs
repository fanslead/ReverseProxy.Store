using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Abstractions;
using Yarp.ReverseProxy.Service;
using System.Collections.Generic;

namespace Yarp.ReverseProxy.Store
{
    public class StoreProxyConfig : IProxyConfig
    {
        public List<ProxyRoute> Routes { get; internal set; } = new List<ProxyRoute>();

        public List<Cluster> Clusters { get; internal set; } = new List<Cluster>();

        IReadOnlyList<ProxyRoute> IProxyConfig.Routes => Routes;

        IReadOnlyList<Cluster> IProxyConfig.Clusters => Clusters;

        public IChangeToken ChangeToken { get; internal set; }

    }
}
