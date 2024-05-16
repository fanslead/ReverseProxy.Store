namespace Yarp.ReverseProxy.Store;

public interface IReverseProxyStore
{
    public event ConfigChangeHandler ChangeConfig;
    IProxyConfig GetConfig();

    void Reload();
    void ReloadConfig();
    IChangeToken GetReloadToken();
}
