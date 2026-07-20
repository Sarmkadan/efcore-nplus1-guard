using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Потокобезопасный класс для хранения скользящего окна выполненных запросов.
/// </summary>
public sealed class QueryTracker
{
    private readonly ConcurrentDictionary<QueryFingerprint, ImmutableList<DateTimeOffset>> _queryTimestamps = new();
    private readonly TimeSpan _detectionWindow;
    private readonly int _threshold;
    private readonly NPlusOneGuardOptions _options;

    /// <summary>
    /// Инициализирует новый экземпляр класса QueryTracker.
    /// </summary>
    /// <param name="options">Параметры детекции N+1.</param>
    public QueryTracker(NPlusOneGuardOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _detectionWindow = options.DetectionWindow;
        _threshold = options.Threshold;
    }

    /// <summary>
    /// Регистрирует выполнение запроса и проверяет на наличие инцидента N+1.
    /// </summary>
    /// <param name="fp">Отпечаток запроса.</param>
    /// <param name="options">Параметры детекции N+1.</param>
    /// <returns>Инцидент N+1, если счётчик в окне превысил порог; иначе null.</returns>
    public NPlusOneIncident? Record(QueryFingerprint fp, NPlusOneGuardOptions options)
    {
        var now = DateTimeOffset.UtcNow;

        // Очистка устаревших записей за пределами DetectionWindow
        CleanupOldRecords(now);

        // Добавление текущего timestamp
        var updatedTimestamps = _queryTimestamps.AddOrUpdate(
            fp,
            _ => ImmutableList.Create(now),
            (_, existing) => existing.Add(now)
        );

        // Проверка на превышение порога
        if (updatedTimestamps.Count >= options.Threshold)
        {
            // Determine severity based on configurable thresholds
            NPlusOneSeverity severity;
            if (updatedTimestamps.Count >= options.MediumSeverityThreshold)
            {
                severity = NPlusOneSeverity.High;
            }
            else if (updatedTimestamps.Count >= options.LowSeverityThreshold)
            {
                severity = NPlusOneSeverity.Medium;
            }
            else
            {
                severity = NPlusOneSeverity.Low;
            }

            var incident = new NPlusOneIncident
            {
                SqlQuery = fp.NormalizedSql,
                Count = updatedTimestamps.Count,
                Severity = severity,
                StackTrace = Environment.StackTrace
            };

            // Capture call site if enabled
            incident.CallSite = ExtractCallSite();

            return incident;
        }

        return null;
    }

    /// <summary>
    /// Extracts the call site from the current stack trace, skipping EF Core, System, Microsoft,
    /// and frames belonging to this library. Returns a formatted string like
    /// "MethodName at FileName:line" or null if no suitable frame is found.
    /// </summary>
    /// <returns>The formatted call site or null.</returns>
    private string? ExtractCallSite()
    {
        if (!_options.CaptureCallSite)
        {
            return null;
        }

        try
        {
            var stack = new StackTrace(true);
            var frames = stack.GetFrames() ?? Array.Empty<StackFrame>();

            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method == null)
                {
                    continue;
                }

                var declaringType = method.DeclaringType;
                if (declaringType == null)
                {
                    continue;
                }

                var typeName = declaringType.FullName ?? declaringType.Name;

                // Skip EF Core frames
                if (typeName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) ||
                    typeName.StartsWith("System.Data", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip generic System namespace frames, but allow common LINQ/Collections/Text namespaces
                if (typeName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) &&
                    !typeName.StartsWith("System.Linq", StringComparison.OrdinalIgnoreCase) &&
                    !typeName.StartsWith("System.Collections", StringComparison.OrdinalIgnoreCase) &&
                    !typeName.StartsWith("System.Text", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip other Microsoft.* frames
                if (typeName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip frames from this library
                if (typeName.StartsWith("EfCoreNPlusOneGuard", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // This is the first application frame
                var methodName = method.Name;
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();

                var callSite = methodName;
                if (!string.IsNullOrEmpty(fileName))
                {
                    // Use only the file name, not the full path, for readability
                    var shortFileName = System.IO.Path.GetFileName(fileName);
                    callSite += " at " + shortFileName;
                    if (lineNumber > 0)
                    {
                        callSite += ":" + lineNumber;
                    }
                }

                return callSite;
            }
        }
        catch
        {
            // Swallow any exception – call site is optional.
        }

        return null;
    }

    /// <summary>
    /// Сбрасывает все сохранённые записи.
    /// </summary>
    public void Reset()
    {
        _queryTimestamps.Clear();
    }

    private void CleanupOldRecords(DateTimeOffset now)
    {
        foreach (var key in _queryTimestamps.Keys.ToList())
        {
            var timestamps = _queryTimestamps[key];
            var cutoff = now.Subtract(_detectionWindow);
            var recentTimestamps = timestamps.Where(t => t >= cutoff).ToImmutableList();

            if (recentTimestamps.IsEmpty)
            {
                _queryTimestamps.TryRemove(key, out _);
            }
            else if (recentTimestamps.Count != timestamps.Count)
            {
                _queryTimestamps[key] = recentTimestamps;
            }
        }
    }

    /// <summary>
    /// Tracks query execution count (backward compatibility method).
    /// </summary>
    /// <param name="commandText">The SQL command text.</param>
    /// <param name="onDetected">Optional callback invoked when an N+1 incident is detected.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandText"/> is <see langword="null"/>.</exception>
    /// <exception cref="NPlusOneDetectedException">Thrown when an incident is detected and <see cref="NPlusOneGuardOptions.ThrowOnDetection"/> is <see langword="true"/>.</exception>
    public void TrackExecution(string commandText, Action<NPlusOneIncident>? onDetected = null)
    {
        if (commandText is null)
        {
            throw new ArgumentNullException(nameof(commandText));
        }

        var fp = QueryFingerprint.Create(commandText, "QueryTracker.TrackExecution");
        var incident = Record(fp, _options);

        if (incident != null)
        {
            onDetected?.Invoke(incident);

            if (_options.LogOnDetection)
            {
                Console.Error.WriteLine("[N+1 Guard] N+1 query pattern detected. Query executed " + incident.Count + " times.");
                Console.Error.WriteLine("[N+1 Guard] SQL: " + commandText.Substring(0, Math.Min(100, commandText.Length)) + "...");
            }

            if (_options.ThrowOnDetection)
            {
                throw new NPlusOneDetectedException(incident);
            }
        }
    }
}
