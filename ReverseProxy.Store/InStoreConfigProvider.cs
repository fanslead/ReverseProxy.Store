namespace Yarp.ReverseProxy.Store;

public class InStoreConfigProvider : IProxyConfigProvider, IDisposable
{
    private readonly object _lockObject = new object();
    private readonly ILogger<InStoreConfigProvider> _logger;
    private readonly IReverseProxyStore _strore;
    private StoreProxyConfig _config;
    private CancellationTokenSource _changeToken;
    private bool _disposed;
    private IDisposable _subscription;

    public InStoreConfigProvider(ILogger<InStoreConfigProvider> logger, IReverseProxyStore strore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strore = strore;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _subscription?.Dispose();
            _changeToken?.Dispose();
            _disposed = true;
        }
    }

    public IProxyConfig GetConfig()
    {
        // First time load
        if (_config == null)
        {
            _subscription = ChangeToken.OnChange(_strore.GetReloadToken, UpdateConfig);
            UpdateConfig();
        }
        return _config;
    }

    [MemberNotNull(nameof(_config))]
    private void UpdateConfig()
    {
        // Prevent overlapping updates, especially on startup.
        lock (_lockObject)
        {
            Log.LoadData(_logger);
            StoreProxyConfig newConfig = null;
            try
            {
                newConfig = _strore.GetConfig() as StoreProxyConfig;
            }
            catch (Exception ex)
            {
                Log.ConfigurationDataConversionFailed(_logger, ex);

                // Re-throw on the first time load to prevent app from starting.
                if (_config == null)
                {
                    throw;
                }

                return;
            }

            var oldToken = _changeToken;
            _changeToken = new CancellationTokenSource();
            newConfig.ChangeToken = new CancellationChangeToken(_changeToken.Token);
            _config = newConfig;

            try
            {
                oldToken?.Cancel(throwOnFirstException: false);
            }
            catch (Exception ex)
            {
                Log.ErrorSignalingChange(_logger, ex);
            }
        }
    }
    private static class Log
    {
        private static readonly Action<ILogger, Exception> _errorSignalingChange = LoggerMessage.Define(
            LogLevel.Error,
            EventIds.ErrorSignalingChange,
            "An exception was thrown from the change notification.");

        private static readonly Action<ILogger, Exception> _loadData = LoggerMessage.Define(
            LogLevel.Information,
            EventIds.LoadData,
            "Loading proxy data from config.");

        private static readonly Action<ILogger, Exception> _configurationDataConversionFailed = LoggerMessage.Define(
            LogLevel.Error,
            EventIds.ConfigurationDataConversionFailed,
            "Configuration data conversion failed.");

        public static void ErrorSignalingChange(ILogger logger, Exception exception)
        {
            _errorSignalingChange(logger, exception);
        }

        public static void LoadData(ILogger logger)
        {
            _loadData(logger, null);
        }

        public static void ConfigurationDataConversionFailed(ILogger logger, Exception exception)
        {
            _configurationDataConversionFailed(logger, exception);
        }
    }
}
