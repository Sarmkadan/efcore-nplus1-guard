using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace EfCoreNPlusOneGuard;

public sealed class NPlusOneGuardInterceptor : DbCommandInterceptor
{
    private readonly NPlusOneGuardOptions _options;
    private readonly Action<NPlusOneIncident>? _onDetected;
    private readonly QueryTracker _tracker;

    public NPlusOneGuardInterceptor(NPlusOneGuardOptions options, Action<NPlusOneIncident>? onDetected = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _onDetected = onDetected;
        _tracker = new QueryTracker(options);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (eventData == null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        TrackQuery(command.CommandText);

        return base.ReaderExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (eventData == null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        await TrackQueryAsync(command.CommandText, cancellationToken).ConfigureAwait(false);

        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private void TrackQuery(string commandText)
    {
        if (commandText == null)
        {
            throw new ArgumentNullException(nameof(commandText));
        }

        foreach (var pattern in _options.IgnoredQueryPatterns)
        {
            if (commandText.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        _tracker.TrackExecution(commandText, _onDetected);
    }

    private ValueTask TrackQueryAsync(string commandText, CancellationToken cancellationToken)
    {
        if (commandText == null)
        {
            throw new ArgumentNullException(nameof(commandText));
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var pattern in _options.IgnoredQueryPatterns)
        {
            if (commandText.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.CompletedTask;
            }
        }

        _tracker.TrackExecution(commandText, _onDetected);
        return ValueTask.CompletedTask;
    }
}