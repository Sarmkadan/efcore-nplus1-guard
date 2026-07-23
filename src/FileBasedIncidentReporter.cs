using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Abstract base class for file-based incident reporters that write N+1 incidents to files.
/// Provides common functionality for path resolution, thread-safe writing, retrying transient
/// IO failures, graceful degradation, and resource management.
/// </summary>
public abstract class FileBasedIncidentReporter : IIncidentReporter, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Backoff delays applied between write retries. A locked file (log shipper, antivirus)
    /// or a transient disk issue is retried up to this many times before the write is
    /// considered failed.
    /// </summary>
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(200),
    ];

    private readonly string _filePath;
    private readonly bool _append;
    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
    private readonly ReporterFailureMode _failureMode;
    private readonly TimeSpan _failureLogInterval;
    private readonly ILogger? _logger;
    private readonly IncidentAggregator? _aggregator;
    private long _lastFailureWarningTicksUtc;
    private bool _fileInitialized;
    private bool _disposed;
    private static readonly System.Text.UTF8Encoding _encoding = new System.Text.UTF8Encoding(false);

    /// <summary>
    /// Gets the path to the output file.
    /// </summary>
    protected string FilePath => _filePath;

    /// <summary>
    /// Gets the encoding used for file operations.
    /// </summary>
    protected System.Text.UTF8Encoding Encoding => _encoding;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBasedIncidentReporter"/> class.
    /// </summary>
    /// <param name="filePath">Path to the output file.</param>
    /// <param name="append">
    /// When <see langword="true"/> (default), existing content in the file is preserved and new
    /// entries are appended. When <see langword="false"/>, the file is truncated the first time
    /// this instance writes to it, then subsequent writes append.
    /// </param>
    /// <param name="options">
    /// Optional guard options used to configure <see cref="ReporterFailureMode"/> and the
    /// rate-limited warning interval. When <see langword="null"/>, the reporter falls back to
    /// <see cref="ReporterFailureMode.LogOnce"/> with a 5 minute warning interval.
    /// </param>
    /// <param name="logger">
    /// Optional logger used to emit a rate-limited warning when writes keep failing under
    /// <see cref="ReporterFailureMode.LogOnce"/>. When <see langword="null"/>, no warning is logged.
    /// </param>
    /// <param name="aggregator">
    /// Optional aggregator whose <see cref="IncidentAggregator.DroppedIncidents"/> metric is
    /// incremented every time an incident cannot be persisted after exhausting retries.
    /// </param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="filePath"/> is null or empty.</exception>
    protected FileBasedIncidentReporter(
        string filePath,
        bool append = true,
        NPlusOneGuardOptions? options = null,
        ILogger? logger = null,
        IncidentAggregator? aggregator = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        _filePath = filePath;
        _append = append;
        _failureMode = options?.ReporterFailureMode ?? ReporterFailureMode.LogOnce;
        _failureLogInterval = options?.ReporterFailureLogInterval ?? TimeSpan.FromMinutes(5);
        _logger = logger;
        _aggregator = aggregator;
    }

    /// <summary>
    /// Reports a single incident to the file.
    /// </summary>
    /// <param name="incident">The incident to report.</param>
    public async void Report(NPlusOneIncident incident)
    {
        if (incident == null)
        {
            throw new ArgumentNullException(nameof(incident));
        }

        await ReportAsync(incident, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Reports a single incident to the file asynchronously.
    /// </summary>
    /// <param name="incident">The incident to report.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReportAsync(NPlusOneIncident incident, CancellationToken cancellationToken = default)
    {
        if (incident == null)
        {
            throw new ArgumentNullException(nameof(incident));
        }

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var line = FormatIncident(incident);
            await WriteLinesAsync(new[] { line }).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Reports multiple incidents to the file.
    /// </summary>
    /// <param name="incidents">The incidents to report.</param>
    public void ReportBatch(IEnumerable<NPlusOneIncident> incidents)
    {
        if (incidents == null)
        {
            throw new ArgumentNullException(nameof(incidents));
        }

        ReportBatchAsync(incidents, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Reports multiple incidents to the file asynchronously.
    /// </summary>
    /// <param name="incidents">The incidents to report.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReportBatchAsync(IEnumerable<NPlusOneIncident> incidents, CancellationToken cancellationToken = default)
    {
        if (incidents == null)
        {
            throw new ArgumentNullException(nameof(incidents));
        }

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var lines = new List<string>();
            foreach (var incident in incidents)
            {
                if (incident != null)
                {
                    lines.Add(FormatIncident(incident));
                }
            }

            if (lines.Count > 0)
            {
                await WriteLinesAsync(lines).ConfigureAwait(false);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Formats a single incident for output.
    /// </summary>
    /// <param name="incident">The incident to format.</param>
    /// <returns>A formatted string representing the incident.</returns>
    protected abstract string FormatIncident(NPlusOneIncident incident);

    /// <summary>
    /// Writes the given lines to the file, honoring the <c>append</c> setting: when
    /// <see langword="false"/>, the file is truncated on the first write of this instance's
    /// lifetime and subsequently appended to; when <see langword="true"/>, existing content
    /// is always preserved. Must be called while holding the write lock.
    /// </summary>
    /// <remarks>
    /// Transient <see cref="IOException"/>s (e.g. the file is momentarily locked by a log
    /// shipper or antivirus) are retried with a short backoff. If the write still fails - be
    /// it an <see cref="IOException"/> after exhausting retries, an
    /// <see cref="UnauthorizedAccessException"/>, or a disk-full condition - the failure is
    /// handled according to the configured <see cref="ReporterFailureMode"/> instead of
    /// propagating and taking down the observed application.
    /// </remarks>
    /// <param name="lines">The lines to write.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="lines"/> is null.</exception>
    protected async Task WriteLinesAsync(IReadOnlyCollection<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        try
        {
            await WriteLinesWithRetryAsync(lines).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            HandleWriteFailure(ex);
        }
    }

    /// <summary>
    /// Attempts to write the given lines to the file, retrying on <see cref="IOException"/>
    /// (e.g. a file locked by a log shipper or antivirus) with a short exponential-ish backoff.
    /// <see cref="UnauthorizedAccessException"/> is not retried, since a permission failure
    /// will not resolve itself within a few hundred milliseconds.
    /// </summary>
    /// <param name="lines">The lines to write.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task WriteLinesWithRetryAsync(IReadOnlyCollection<string> lines)
    {
        var truncate = !_append && !_fileInitialized;

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                if (truncate)
                {
                    // Write header and first data line
                    var header = GetHeader();
                    await File.WriteAllLinesAsync(_filePath, header.Concat(lines), _encoding).ConfigureAwait(false);
                }
                else
                {
                    await File.AppendAllLinesAsync(_filePath, lines, _encoding).ConfigureAwait(false);
                }

                _fileInitialized = true;
                return;
            }
            catch (IOException) when (attempt < RetryDelays.Length)
            {
                await Task.Delay(RetryDelays[attempt]).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Handles a write failure that survived retries by degrading gracefully according to the
    /// configured <see cref="ReporterFailureMode"/>: the drop is always counted on the
    /// aggregator (when one was supplied), and depending on the mode the failure is either
    /// swallowed silently, swallowed with a rate-limited warning log, or rethrown.
    /// </summary>
    /// <param name="exception">The exception that caused the write to fail.</param>
    /// <exception cref="IOException">
    /// Rethrown when <see cref="ReporterFailureMode"/> is <see cref="ReporterFailureMode.Throw"/>
    /// and the original failure was an <see cref="IOException"/>.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Rethrown when <see cref="ReporterFailureMode"/> is <see cref="ReporterFailureMode.Throw"/>
    /// and the original failure was an <see cref="UnauthorizedAccessException"/>.
    /// </exception>
    private void HandleWriteFailure(Exception exception)
    {
        _aggregator?.RecordDroppedIncident();

        switch (_failureMode)
        {
            case ReporterFailureMode.Throw:
                throw exception;
            case ReporterFailureMode.Silent:
                return;
            case ReporterFailureMode.LogOnce:
            default:
                LogFailureRateLimited(exception);
                return;
        }
    }

    /// <summary>
    /// Logs a warning for a dropped incident at most once per <see cref="_failureLogInterval"/>,
    /// so that a sustained outage produces one warning per interval instead of one per incident.
    /// </summary>
    /// <param name="exception">The exception that caused the write to fail.</param>
    private void LogFailureRateLimited(Exception exception)
    {
        if (_logger is null)
        {
            return;
        }

        var nowTicks = DateTime.UtcNow.Ticks;
        var lastTicks = Interlocked.Read(ref _lastFailureWarningTicksUtc);

        if (nowTicks - lastTicks < _failureLogInterval.Ticks)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _lastFailureWarningTicksUtc, nowTicks, lastTicks) != lastTicks)
        {
            // Another thread just logged; skip to honor the rate limit.
            return;
        }

        _logger.LogWarning(
            exception,
            "N+1 incident reporter failed to write to '{FilePath}' and is dropping incidents. Further failures are suppressed for {Interval}.",
            _filePath,
            _failureLogInterval);
    }

    /// <summary>
    /// Gets the header lines for the file (if applicable).
    /// </summary>
    /// <returns>An enumerable of header lines, or empty if no header is needed.</returns>
    protected virtual IEnumerable<string> GetHeader()
    {
        yield break;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources;
    /// <see langword="false"/> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _writeLock?.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Releases resources asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
        {
            return;
        }

        if (_writeLock != null)
        {
            _writeLock.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Finalizer to ensure resources are released if not disposed properly.
    /// </summary>
    ~FileBasedIncidentReporter()
    {
        Dispose(false);
    }
}
