# ReverseProxy.Store
yarp用EFCore存储配置

# 例子使用说明
## 先配置数据库链接字符串，然后执行code first还原数据库，然后就可以用了。
appsettings.json加上下面东西，Password是前端登录验证密码（做做样子的233333）
```
  "ConnectionStrings": {
    "Default": ""
  },
    "Password": "password"
```
# 单独使用ReverseProxy.Store
在Startup.cs中配置
```
using ReverseProxy.Store.EFCore;
using ReverseProxy.Store.EFCore.Management;
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<EFCoreDbContext>(options =>
                    options.UseMySql(
                        Configuration.GetConnectionString("Default"),
                        ServerVersion.AutoDetect(Configuration.GetConnectionString("Default")),
                        b => b.MigrationsAssembly("EFCoreSample")));
    services.AddTransient<IClusterManagement, ClusterManagement>();
    services.AddTransient<IProxyRouteManagement, ProxyRouteManagement>();
    services.AddReverseProxy()
            .LoadFromEFCore();
}
```
然后就可以自己实现业务API管理配置内容啦~~~
