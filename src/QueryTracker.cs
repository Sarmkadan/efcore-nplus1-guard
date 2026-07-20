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
            incident.CallSite = ExtractCallSite(Environment.StackTrace);

            return incident;
        }

        return null;
    }

    /// <summary>
    /// Extracts the call site from the stack trace, skipping EF Core, System, and this library's frames.
    /// </summary>
    /// <param name="stackTrace">The full stack trace string.</param>
    /// <returns>A formatted call site string ("MethodName at FileName:line") or null if no application frame found.</returns>
    private string? ExtractCallSite(string stackTrace)
    {
        if (!_options.CaptureCallSite || string.IsNullOrEmpty(stackTrace))
        {
            return null;
        }

        try
        {
            // Parse the stack trace string to extract frames
            // Stack trace format: "at Namespace.Type.Method (file:line)"
            var lines = stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("at ", StringComparison.Ordinal))
                {
                    continue;
                }

                // Extract the method signature part
                var methodPart = trimmed.Substring(3).Trim();

                // Find the end of the method signature (before '(' or space)
                var endIndex = methodPart.IndexOf('(');
                if (endIndex < 0)
                {
                    endIndex = methodPart.IndexOf(' ');
                }
                if (endIndex < 0)
                {
                    endIndex = methodPart.Length;
                }

                var fullMethod = methodPart.Substring(0, endIndex).Trim();

                // Split into type and method
                var lastDot = fullMethod.LastIndexOf('.');
                if (lastDot < 0)
                {
                    continue;
                }

                var typeName = fullMethod.Substring(0, lastDot);
                var methodName = fullMethod.Substring(lastDot + 1);

                // Extract file and line info from the part after '('
                string? fileName = null;
                int lineNumber = 0;

                var openParenIndex = methodPart.IndexOf('(');
                if (openParenIndex >= 0)
                {
                    var fileInfoPart = methodPart.Substring(openParenIndex + 1);
                    var closeParenIndex = fileInfoPart.IndexOf(')');
                    if (closeParenIndex > 0)
                    {
                        var fileInfo = fileInfoPart.Substring(0, closeParenIndex);
                        var parts = fileInfo.Split(':');
                        if (parts.Length >= 2)
                        {
                            fileName = parts[0];
                            if (int.TryParse(parts[1], out var lineNum))
                            {
                                lineNumber = lineNum;
                            }
                        }
                        else if (parts.Length == 1)
                        {
                            fileName = parts[0];
                        }
                    }
                }

                // Skip EF Core frames
                if (typeName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) ||
                    typeName.StartsWith("System.Data", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip System namespace frames (but allow System.Linq, System.Collections, etc. that might be in this library)
                if (typeName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) &&
                    !typeName.StartsWith("System.Linq", StringComparison.OrdinalIgnoreCase) &&
                    !typeName.StartsWith("System.Collections", StringComparison.OrdinalIgnoreCase) &&
                    !typeName.StartsWith("System.Text", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip Microsoft.* frames
                if (typeName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip frames from this library
                if (typeName.StartsWith("EfCoreNPlusOneGuard", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Found an application frame - format the call site
                if (string.IsNullOrEmpty(methodName))
                {
                    continue;
                }

                // Format: MethodName at FileName:line
                var callSite = methodName;
                if (!string.IsNullOrEmpty(fileName))
                {
                    callSite += " at " + fileName;
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
            // If anything goes wrong with call site extraction, just return null
            // The incident will still be created with the full stack trace
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
