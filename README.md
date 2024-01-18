# ReverseProxy.Store
![image](https://img.shields.io/nuget/dt/ReverseProxy.Store)
![image](https://img.shields.io/github/license/fanslead/ReverseProxy.Store)
![image](https://img.shields.io/github/v/release/fanslead/ReverseProxy.Store)
![image](https://img.shields.io/github/stars/fanslead/ReverseProxy.Store?style=social)
![image](https://img.shields.io/github/forks/fanslead/ReverseProxy.Store?style=social)
## 先看界面
![1639733073(1)](https://user-images.githubusercontent.com/22066473/146521329-9c8d04b4-dc99-47a0-87bc-cc081e9b5cc4.jpg)
![image](https://user-images.githubusercontent.com/22066473/146521423-48df866b-4299-4628-b6aa-c8d4fbfcbe43.png)

yarp用EFCore存储配置
安裝
> Install-Package ReverseProxy.Store.EFCore -Version 2.0.0

或者
> dotnet add package ReverseProxy.Store.EFCore --version 2.0.0

分布式同步配置更新
安裝
> Install-Package ReverseProxy.Store.Distributed -Version 2.0.0

或者
> dotnet add package ReverseProxy.Store.Distributed --version 2.0.0

# 使用界面
## 使用说明
### 先配置数据库链接字符串，然后执行code first还原数据库，然后就可以用了。
appsettings.json加上下面东西，Password是前端登录验证密码（做做样子的233333）
```
  "ConnectionStrings": {
    "Default": ""
  },
    "Password": "password"
```
### 使用ReverseProxy.WebApi
配置
```
using ReverseProxy.Store.EFCore;
using ReverseProxy.Store.EFCore.Management;
public void ConfigureServices(IServiceCollection services)
{
    services.AddMemoryCache();
    services.AddDbContext<EFCoreDbContext>(options =>
                    options.UseMySql(
                        Configuration.GetConnectionString("Default"),
                        ServerVersion.AutoDetect(Configuration.GetConnectionString("Default")),
                        b => b.MigrationsAssembly("EFCoreSample")));
    services.AddTransient<IClusterManagement, ClusterManagement>();
    services.AddTransient<IProxyRouteManagement, ProxyRouteManagement>();
    services.AddReverseProxy()
            .LoadFromEFCore()
             //.AddRedis("127.0.0.1:6379"); //使用redis同步配置更新
}
```
然后就可以自己实现业务API管理配置内容啦~~~

### 启动ReverseProxy.Dashboard
修改public目录下的config.js可以配置请求后端路径，默认如下
```
const orign = 'http://localhost:5201'
const apiConfig = {
  baseURL: orign
};
```
