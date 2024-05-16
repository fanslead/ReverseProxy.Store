namespace ReverseProxy.Store.Entities;

public class SessionAffinityCookie
{
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// The cookie path.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// The domain to associate the cookie with.
    /// </summary>
    public string? Domain { get; init; }

    /// <summary>
    /// Indicates whether a cookie is accessible by client-side script.
    /// </summary>
    /// <remarks>Defaults to "true".</remarks>
    public bool? HttpOnly { get; init; }

    /// <summary>
    /// The policy that will be used to determine <see cref="CookieOptions.Secure"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="CookieSecurePolicy.None"/>.</remarks>
    public CookieSecurePolicy? SecurePolicy { get; init; }

    /// <summary>
    /// The SameSite attribute of the cookie.
    /// </summary>
    /// <remarks>Defaults to <see cref="SameSiteMode.Unspecified"/>.</remarks>
    public SameSiteMode? SameSite { get; init; }

    /// <summary>
    /// Gets or sets the lifespan of a cookie.
    /// </summary>
    public string? Expiration { get; init; }

    /// <summary>
    /// Gets or sets the max-age for the cookie.
    /// </summary>
    public string? MaxAge { get; init; }

    /// <summary>
    /// Indicates if this cookie is essential for the application to function correctly. If true then
    /// consent policy checks may be bypassed.
    /// </summary>
    /// <remarks>Defaults to "false".</remarks>
    public bool? IsEssential { get; init; }

    public int SessionAffinityConfigId { get; init; }
    public virtual SessionAffinityConfig SessionAffinityConfig { get; init; }

    public bool Equals(SessionAffinityCookie? other)
    {
        if (other == null)
        {
            return false;
        }

        return string.Equals(Path, other.Path, StringComparison.Ordinal)
            && string.Equals(Domain, other.Domain, StringComparison.OrdinalIgnoreCase)
            && HttpOnly == other.HttpOnly
            && SecurePolicy == other.SecurePolicy
            && SameSite == other.SameSite
            && Expiration == other.Expiration
            && MaxAge == other.MaxAge
            && IsEssential == other.IsEssential;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Path?.GetHashCode(StringComparison.Ordinal),
            Domain?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            HttpOnly,
            SecurePolicy,
            SameSite,
            Expiration,
            MaxAge,
            IsEssential);
    }
}
