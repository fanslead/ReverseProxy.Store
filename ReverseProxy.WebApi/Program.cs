using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ReverseProxy.Store.Distributed.Redis;
using ReverseProxy.Store.EFCore.Revisions;
using ReverseProxy.WebApi.Application.Configuration;
using ReverseProxy.WebApi.Data;
using ReverseProxy.WebApi.Health;
using ReverseProxy.WebApi.Middleware;
using ReverseProxy.WebApi.Security;

var builder = WebApplication.CreateBuilder(args);
var useSecureCookies = !builder.Environment.IsDevelopment();

builder.Services.AddDbContextFactory<ApplicationDbContext>((services, options) =>
{
    var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:Default must contain a PostgreSQL connection string.");
    }

    options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("ReverseProxy.WebApi"));
});
builder.Services.AddDataProtection()
    .SetApplicationName("ReverseProxy.Store.ControlPlane")
    .PersistKeysToDbContext<ApplicationDbContext>();
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = useSecureCookies ? "__Host-reverseproxy.auth" : "reverseproxy.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = useSecureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanView", policy => policy.RequireRole(ControlPlaneRoles.All))
    .AddPolicy("CanEdit", policy => policy.RequireRole(ControlPlaneRoles.Editor, ControlPlaneRoles.Administrator))
    .AddPolicy("CanPublish", policy => policy.RequireRole(ControlPlaneRoles.Publisher, ControlPlaneRoles.Administrator))
    .AddPolicy("CanAdminister", policy => policy.RequireRole(ControlPlaneRoles.Administrator));
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = useSecureCookies ? "__Host-reverseproxy.csrf" : "reverseproxy.csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = useSecureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
    options.AddFixedWindowLimiter("publish", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddOpenApi("v1");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ConfigOperationFinalizer>();
builder.Services.AddScoped<IConfigDraftService, ConfigDraftService>();
builder.Services.AddScoped<IConfigRevisionService, ConfigRevisionService>();
builder.Services.AddOptions<ConfigOperationRecoveryOptions>()
    .Bind(builder.Configuration.GetSection(ConfigOperationRecoveryOptions.SectionName))
    .Validate(options => options.BatchSize is > 0 and <= 1000, "Operations:BatchSize must be between 1 and 1000.")
    .Validate(options => options.PollInterval > TimeSpan.Zero, "Operations:PollInterval must be positive.")
    .ValidateOnStart();
builder.Services.AddHostedService<ConfigOperationRecoveryWorker>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), ["live"])
    .AddCheck<ControlPlaneReadinessHealthCheck>("control-plane", tags: ["ready"]);
var reverseProxyBuilder = builder.Services
    .AddReverseProxy()
    .LoadFromEFCoreRevisionStore<ApplicationDbContext>();
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddOptions<RedisProxyConfigOptions>()
        .Bind(builder.Configuration.GetSection("Redis"));
    reverseProxyBuilder.AddRedisRevisionDistribution(
        redisConnectionString,
        builder.Configuration["Redis:ChannelName"]);
    builder.Services.AddOptions<ConfigOutboxOptions>()
        .Bind(builder.Configuration.GetSection(ConfigOutboxOptions.SectionName))
        .Validate(options => options.BatchSize is > 0 and <= 1000, "Outbox:BatchSize must be between 1 and 1000.")
        .Validate(options => options.PollInterval > TimeSpan.Zero, "Outbox:PollInterval must be positive.")
        .Validate(options => options.FailureDelay > TimeSpan.Zero, "Outbox:FailureDelay must be positive.")
        .ValidateOnStart();
    builder.Services.AddSingleton<ConfigOutboxDispatcher>();
    builder.Services.AddHostedService<ConfigOutboxWorker>();
}

var app = builder.Build();
await ControlPlaneInitializer.InitializeAsync(
    app.Services,
    app.Configuration,
    app.Environment,
    app.Lifetime.ApplicationStopping);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHsts();
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).AllowAnonymous();
app.MapReverseProxy();

app.Run();

public partial class Program;
