using ReverseProxy.WebApi.Validator;
using FluentValidation;
using FluentValidation.AspNetCore;
using ReverseProxy.Store.Entity;
using ReverseProxy.Store.EFCore;
using Microsoft.EntityFrameworkCore;
using ReverseProxy.Store.EFCore.Management;
using Newtonsoft.Json;
using Microsoft.OpenApi.Models;
using Yarp.ReverseProxy.Model;
using ReverseProxy.Store.Distributed.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors();
builder.Services.AddMemoryCache();
// Ìí¼ÓÑéÖ¤Æ÷
builder.Services.AddSingleton<IValidator<Cluster>, ClusterValidator>();
builder.Services.AddSingleton<IValidator<ProxyRoute>, ProxyRouteValidator>();


//mysql
//builder.Services.AddDbContext<EFCoreDbContext>(options =>
//        options.UseMySql(
//            Configuration.GetConnectionString("Default"),
//            ServerVersion.AutoDetect(Configuration.GetConnectionString("Default")),
//            b => b.MigrationsAssembly("ReverseProxy.WebApi")));

builder.Services.AddDbContext<EFCoreDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("Default"),
            b => b.MigrationsAssembly("ReverseProxy.WebApi")));

builder.Services.AddTransient<IClusterManagement, ClusterManagement>();
builder.Services.AddTransient<IProxyRouteManagement, ProxyRouteManagement>();
builder.Services.AddControllers()
    .AddFluentValidation()
    .AddNewtonsoftJson(options => {
                     options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                     options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                 }); ;

builder.Services.AddReverseProxy()
    .LoadFromEFCore()
    .AddRedis("127.0.0.1:6379");
    ;
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ReverseProxy.WebApi", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ReverseProxy.WebApi v1"));
}
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
});

app.Run();
