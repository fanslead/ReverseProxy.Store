namespace Yarp.ReverseProxy.Store.Revisions;

public interface IProxyConfigEventPublisher
{
    ValueTask PublishAsync(ProxyConfigPublishedEvent publishedEvent, CancellationToken cancellationToken);
}