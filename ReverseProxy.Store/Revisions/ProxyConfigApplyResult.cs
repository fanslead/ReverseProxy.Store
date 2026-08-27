namespace Yarp.ReverseProxy.Store.Revisions;

public enum ProxyConfigApplyStatus
{
    Applied,
    Ignored,
    Failed
}

public sealed record ProxyConfigApplyResult(
    ProxyConfigApplyStatus Status,
    long? RevisionNumber,
    string? ErrorCode = null);