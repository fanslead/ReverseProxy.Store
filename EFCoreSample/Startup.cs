using EFCoreSample.Validator;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Yarp.ReverseProxy.Middleware;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ReverseProxy.Store.EFCore;
using ReverseProxy.Store.EFCore.Management;
using VueCliMiddleware;
using ReverseProxy.Store.Entity;

namespace EFCoreSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSpaStaticFiles(config => {
                config.RootPath = "ClientApp/dist"; //此处build 对应vue项目的发布文件夹名。
                                                     //如未配置vue项目的build文件夹名，则需修改为dist
            });
            services.AddCors();
            services.AddMemoryCache();
            // 添加验证器
            services.AddSingleton<IValidator<Cluster>, ClusterValidator>();
            services.AddSingleton<IValidator<ProxyRoute>, ProxyRouteValidator>();
            services.AddDbContext<EFCoreDbContext>(options =>
                    options.UseMySql(
                        Configuration.GetConnectionString("Default"),
                        ServerVersion.AutoDetect(Configuration.GetConnectionString("Default")),
                        b => b.MigrationsAssembly("EFCoreSample")));
            services.AddTransient<IClusterManagement, ClusterManagement>();
            services.AddTransient<IProxyRouteManagement, ProxyRouteManagement>();
            services.AddControllers()
                 .AddFluentValidation()
                 .AddNewtonsoftJson(options => {
                     options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                     options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                 });
            services.AddReverseProxy()
                .LoadFromEFCore()
                .AddConfigFilter<CustomConfigFilter>();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "EFCoreSample", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EFCoreSample v1"));

            #region 添加单页面web应用  vue
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            #endregion
            app.UseCors(builder => {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    //.AllowCredentials()
                    ;
                });
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapReverseProxy(proxyPipeline =>
                {
                    // Custom endpoint selection
                    proxyPipeline.Use((context, next) =>
                    {
                        var someCriteria = false; // MeetsCriteria(context);
                        if (someCriteria)
                        {
                            var availableDestinationsFeature = context.Features.Get<IReverseProxyFeature>();
                            var destination = availableDestinationsFeature.AvailableDestinations[0]; // PickDestination(availableDestinationsFeature.Destinations);
                            // Load balancing will no-op if we've already reduced the list of available destinations to 1.
                            availableDestinationsFeature.AvailableDestinations = destination;
                        }

                        return next();
                    });
                    proxyPipeline.UseSessionAffinity();
                    proxyPipeline.UseLoadBalancing();
                    proxyPipeline.UsePassiveHealthChecks();
                })
               .ConfigureEndpoints((builder, route) => builder.WithDisplayName($"ReverseProxy {route.RouteId}-{route.ClusterId}"));

                endpoints.MapToVueCliProxy(
                    "{*path}",
                    new SpaOptions { SourcePath = "ClientApp" },
                    npmScript: (System.Diagnostics.Debugger.IsAttached) ? "dev" : null,
                    regex: "Compiled successfully",
                    forceKill: true
                    );
            });

        }
    }
}
