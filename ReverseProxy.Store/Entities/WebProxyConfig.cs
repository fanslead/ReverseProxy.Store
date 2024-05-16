namespace ReverseProxy.Store.Entities;


/// <summary>
/// Config used to construct <seealso cref="System.Net.WebProxy"/> instance.
/// </summary>
public class WebProxyConfig : IEquatable<WebProxyConfig>
{
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// The URI of the proxy server.
    /// </summary>
    public string Address { get; init; }

    /// <summary>
    /// true to bypass the proxy for local addresses; otherwise, false.
    /// If null, default value will be used: false
    /// </summary>
    public bool? BypassOnLocal { get; init; }

    /// <summary>
    /// Controls whether the <seealso cref="System.Net.CredentialCache.DefaultCredentials"/> are sent with requests.
    /// If null, default value will be used: false
    /// </summary>
    public bool? UseDefaultCredentials { get; init; }
    public int HttpClientConfigId { get; init; }
    public virtual HttpClientConfig HttpClientConfig { get; init; }



    public bool Equals(WebProxyConfig? other)
    {
        if (other == null)
        {
            return false;
        }

        return Address == other.Address
            && BypassOnLocal == other.BypassOnLocal
            && UseDefaultCredentials == other.UseDefaultCredentials;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Address,
            BypassOnLocal,
            UseDefaultCredentials
        );
    }
}
