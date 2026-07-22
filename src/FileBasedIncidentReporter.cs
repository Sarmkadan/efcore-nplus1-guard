using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Abstract base class for file-based incident reporters that write N+1 incidents to files.
/// Provides common functionality for path resolution, thread-safe writing, and resource management.
/// </summary>
public abstract class FileBasedIncidentReporter : IIncidentReporter, IDisposable, IAsyncDisposable
{
    private readonly string _filePath;
    private readonly bool _append;
    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
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
    protected FileBasedIncidentReporter(string filePath, bool append = true)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _append = append;
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
    /// <param name="lines">The lines to write.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task WriteLinesAsync(IReadOnlyCollection<string> lines)
    {
        var truncate = !_append && !_fileInitialized;

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
