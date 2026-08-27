namespace Yarp.ReverseProxy.Store.Revisions;

public interface IProxyConfigRevisionStore
{
    ValueTask<ProxyConfigRevision> LoadActiveRevisionAsync(CancellationToken cancellationToken);
}