using Microsoft.AspNetCore.SpaServices;
using VueCliMiddleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSpaStaticFiles(config =>
{
    config.RootPath = "ClientApp/dist"; //此处build 对应vue项目的发布文件夹名。
});

var app = builder.Build();

#region 添加单页面web应用  vue
app.UseStaticFiles();
app.UseSpaStaticFiles();
#endregion
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapToVueCliProxy(
                       "{*path}",
                       new SpaOptions { SourcePath = "ClientApp" },
                       npmScript: (System.Diagnostics.Debugger.IsAttached) ? "dev" : null,
                       regex: "Compiled successfully",
                       forceKill: true
                       );
});
app.Run();
