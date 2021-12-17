using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Store;

namespace ReverseProxy.Store.Distributed.Redis
{
    public static class RedisServiceCollectionExtension
    {
        public static IReverseProxyBuilder AddRedis(this IReverseProxyBuilder builder, string connetionString)
        {
            builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(connetionString));
            builder.Services.AddHostedService(sp => new ConfigChangeSubscriber(sp.GetRequiredService<IReverseProxyStore>(), ConnectionMultiplexer.Connect(connetionString)));
            return builder;
        }
    }
}
