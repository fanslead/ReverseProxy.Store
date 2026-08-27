var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine("ClientApp", "dist")
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; connect-src 'self'; font-src 'self' data:; img-src 'self' data:; " +
        "script-src 'self'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'; " +
        "base-uri 'self'; form-action 'self'";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl =
            context.Context.Request.Path.StartsWithSegments("/assets")
                ? "public,max-age=31536000,immutable"
                : "no-cache";
    }
});

app.MapReverseProxy();
app.MapFallbackToFile("index.html");
app.Run();
