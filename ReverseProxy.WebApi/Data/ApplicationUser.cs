using Microsoft.AspNetCore.Identity;

namespace ReverseProxy.WebApi.Data;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}