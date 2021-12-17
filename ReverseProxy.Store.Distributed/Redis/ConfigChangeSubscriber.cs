using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Store;

namespace ReverseProxy.Store.Distributed.Redis
{
    public class ConfigChangeSubscriber : BackgroundService
    {
        private readonly IReverseProxyStore _reverseProxyStore;

        public ConfigChangeSubscriber(IReverseProxyStore reverseProxyStore, IConnectionMultiplexer connectionMultiplexer)
        {
            _reverseProxyStore = reverseProxyStore;
            var sub = connectionMultiplexer.GetSubscriber();
            sub.Subscribe("ConfigChange", (channel, value) => _reverseProxyStore.ReloadConfig());
            _reverseProxyStore.ChangeConfig -= _reverseProxyStore.ReloadConfig;
            _reverseProxyStore.ChangeConfig += () => 
                sub.Publish("ConfigChange", "")
            ;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}
