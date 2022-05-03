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
            var incident = new NPlusOneIncident
            {
                SqlQuery = fp.NormalizedSql,
                Count = updatedTimestamps.Count,
                Severity = NPlusOneSeverity.High,
                StackTrace = Environment.StackTrace
            };
            return incident;
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
