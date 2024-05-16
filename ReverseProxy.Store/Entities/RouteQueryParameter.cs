namespace ReverseProxy.Store.Entities;

public class RouteQueryParameter
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Name of the query parameter to look for.
    /// This field is case insensitive and required.
    /// </summary>
    public string Name { get; init; } = default!;

    /// <summary>
    /// A collection of acceptable query parameter values used during routing.
    /// </summary>
    public string? Values { get; init; }

    /// <summary>
    /// Specifies how query parameter values should be compared (e.g. exact matches Vs. contains).
    /// Defaults to <see cref="QueryParameterMatchMode.Exact"/>.
    /// </summary>
    public QueryParameterMatchMode Mode { get; init; }

    /// <summary>
    /// Specifies whether query parameter value comparisons should ignore case.
    /// When <c>true</c>, <see cref="StringComparison.Ordinal" /> is used.
    /// When <c>false</c>, <see cref="StringComparison.OrdinalIgnoreCase" /> is used.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool IsCaseSensitive { get; init; }
    public int ProxyMatchId { get; set; }
    public virtual ProxyMatch ProxyMatch { get; set; }
}
