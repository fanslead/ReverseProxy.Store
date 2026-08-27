namespace Yarp.ReverseProxy.Store.Configuration;

public sealed record ProxyConfigResourceDiff(
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Modified)
{
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Modified.Count > 0;
}

public sealed record ProxyConfigDocumentDiff(
    string BaseHash,
    string CandidateHash,
    ProxyConfigResourceDiff Routes,
    ProxyConfigResourceDiff Clusters)
{
    public bool HasChanges => Routes.HasChanges || Clusters.HasChanges;
}

public static class ProxyConfigDocumentDiffer
{
    public static ProxyConfigDocumentDiff Compare(
        ProxyConfigDocument baseline,
        ProxyConfigDocument candidate)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);

        return new ProxyConfigDocumentDiff(
            ProxyConfigDocumentSerializer.ComputeHash(baseline),
            ProxyConfigDocumentSerializer.ComputeHash(candidate),
            CompareResources(
                baseline.Routes,
                candidate.Routes,
                route => route.RouteId,
                route => ProxyConfigDocumentSerializer.ComputeHash(new ProxyConfigDocument { Routes = [route] })),
            CompareResources(
                baseline.Clusters,
                candidate.Clusters,
                cluster => cluster.ClusterId,
                cluster => ProxyConfigDocumentSerializer.ComputeHash(new ProxyConfigDocument { Clusters = [cluster] })));
    }

    private static ProxyConfigResourceDiff CompareResources<TResource>(
        IEnumerable<TResource> baseline,
        IEnumerable<TResource> candidate,
        Func<TResource, string> getId,
        Func<TResource, string> getHash)
    {
        var baselineResources = CreateResourceFingerprints(baseline, getId, getHash);
        var candidateResources = CreateResourceFingerprints(candidate, getId, getHash);
        var added = candidateResources.Keys
            .Except(baselineResources.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var removed = baselineResources.Keys
            .Except(candidateResources.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var modified = baselineResources.Keys
            .Intersect(candidateResources.Keys, StringComparer.Ordinal)
            .Where(id => !string.Equals(baselineResources[id], candidateResources[id], StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new ProxyConfigResourceDiff(added, removed, modified);
    }

    private static Dictionary<string, string> CreateResourceFingerprints<TResource>(
        IEnumerable<TResource> resources,
        Func<TResource, string> getId,
        Func<TResource, string> getHash)
    {
        return resources
            .GroupBy(resource => getId(resource) ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => string.Join('|', group.Select(getHash).Order(StringComparer.Ordinal)),
                StringComparer.Ordinal);
    }
}