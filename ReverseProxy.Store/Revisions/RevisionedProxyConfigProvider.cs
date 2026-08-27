using Yarp.ReverseProxy.Store.Configuration;

namespace Yarp.ReverseProxy.Store.Revisions;

public sealed partial class RevisionedProxyConfigProvider : IProxyConfigProvider, IDisposable
{
    private readonly IProxyConfigRevisionStore _revisionStore;
    private readonly ILogger<RevisionedProxyConfigProvider> _logger;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private ProviderState? _state;
    private int _disposed;

    public RevisionedProxyConfigProvider(
        IProxyConfigRevisionStore revisionStore,
        ILogger<RevisionedProxyConfigProvider> logger)
    {
        _revisionStore = revisionStore ?? throw new ArgumentNullException(nameof(revisionStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IProxyConfig GetConfig()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var currentState = Volatile.Read(ref _state);
        if (currentState is not null)
        {
            return currentState.Config;
        }

        ReloadAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
        return Volatile.Read(ref _state)?.Config
            ?? throw new InvalidOperationException("No active proxy configuration revision is available.");
    }

    public async ValueTask<ProxyConfigApplyResult> ReloadAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
        await _reloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var revision = await _revisionStore.LoadActiveRevisionAsync(cancellationToken).ConfigureAwait(false);
            ValidateRevision(revision);

            var currentState = Volatile.Read(ref _state);
            if (currentState is not null)
            {
                if (revision.RevisionNumber < currentState.Config.RevisionNumber)
                {
                    LogOlderRevisionIgnored(_logger, revision.RevisionNumber, currentState.Config.RevisionNumber);
                    return new(ProxyConfigApplyStatus.Ignored, revision.RevisionNumber);
                }

                if (revision.RevisionNumber == currentState.Config.RevisionNumber)
                {
                    if (!string.Equals(revision.ContentHash, currentState.Config.ContentHash, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException(
                            $"Revision {revision.RevisionNumber} changed content hash and is not immutable.");
                    }

                    return new(ProxyConfigApplyStatus.Ignored, revision.RevisionNumber);
                }
            }

            var canonicalJson = ProxyConfigDocumentSerializer.SerializeCanonical(revision.Document);
            var materializedDocument = ProxyConfigDocumentSerializer.Deserialize(canonicalJson);
            var nextTokenSource = new CancellationTokenSource();
            var nextConfig = new RevisionedProxyConfig(
                revision,
                materializedDocument.Routes.ToArray(),
                materializedDocument.Clusters.ToArray(),
                new CancellationChangeToken(nextTokenSource.Token));
            var previousState = Interlocked.Exchange(ref _state, new ProviderState(nextConfig, nextTokenSource));

            SignalChange(previousState);
            LogRevisionApplied(_logger, revision.RevisionNumber, revision.ContentHash);
            return new(ProxyConfigApplyStatus.Applied, revision.RevisionNumber);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (Volatile.Read(ref _state) is not null)
        {
            var currentRevision = Volatile.Read(ref _state)!.Config.RevisionNumber;
            LogRevisionApplyFailed(_logger, currentRevision, exception);
            return new(ProxyConfigApplyStatus.Failed, currentRevision, "configuration_apply_failed");
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        var state = Interlocked.Exchange(ref _state, null);
        SignalChange(state);
        _reloadLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void ValidateRevision(ProxyConfigRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);

        if (revision.RevisionId == Guid.Empty)
        {
            throw new InvalidDataException("The active proxy configuration revision ID is empty.");
        }

        if (revision.RevisionNumber < 0)
        {
            throw new InvalidDataException("The active proxy configuration revision number cannot be negative.");
        }

        var validation = ProxyConfigDocumentValidator.Validate(revision.Document);
        if (!validation.IsValid)
        {
            var details = string.Join("; ", validation.Errors.Select(error => $"{error.Path}: {error.Message}"));
            throw new InvalidDataException($"The active proxy configuration revision is invalid. {details}");
        }

        var computedHash = ProxyConfigDocumentSerializer.ComputeHash(revision.Document);
        if (!string.Equals(computedHash, revision.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"The active proxy configuration revision hash does not match its document. Expected '{revision.ContentHash}', computed '{computedHash}'.");
        }
    }

    private void SignalChange(ProviderState? state)
    {
        if (state is null)
        {
            return;
        }

        try
        {
            state.ChangeTokenSource.Cancel(throwOnFirstException: false);
        }
        catch (Exception exception)
        {
            LogChangeSignalFailed(_logger, exception);
        }
        finally
        {
            state.ChangeTokenSource.Dispose();
        }
    }

    [LoggerMessage(4, LogLevel.Information, "Applied proxy configuration revision {RevisionNumber} with hash {ContentHash}.")]
    private static partial void LogRevisionApplied(ILogger logger, long revisionNumber, string contentHash);

    [LoggerMessage(5, LogLevel.Debug, "Ignored proxy configuration revision {RevisionNumber}; revision {CurrentRevisionNumber} is already applied.")]
    private static partial void LogOlderRevisionIgnored(ILogger logger, long revisionNumber, long currentRevisionNumber);

    [LoggerMessage(6, LogLevel.Error, "Failed to apply proxy configuration. Revision {CurrentRevisionNumber} remains active.")]
    private static partial void LogRevisionApplyFailed(ILogger logger, long currentRevisionNumber, Exception exception);

    [LoggerMessage(7, LogLevel.Error, "An exception was thrown while signaling a proxy configuration change.")]
    private static partial void LogChangeSignalFailed(ILogger logger, Exception exception);

    private sealed record ProviderState(
        RevisionedProxyConfig Config,
        CancellationTokenSource ChangeTokenSource);
}