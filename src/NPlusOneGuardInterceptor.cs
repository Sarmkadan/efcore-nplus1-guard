// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// EF Core interceptor that detects N+1 query patterns.
/// </summary>
internal class NPlusOneGuardInterceptor : DbCommandInterceptor
{
    private readonly NPlusOneGuardOptions _options;
    private readonly Action<NPlusOneIncident>? _onDetected;
    private readonly QueryTracker _tracker;

    public NPlusOneGuardInterceptor(
        NPlusOneGuardOptions options,
        Action<NPlusOneIncident>? onDetected = null)
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
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (eventData is null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        TrackQuery(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (eventData is null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        TrackQuery(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void TrackQuery(string commandText)
    {
        if (commandText is null)
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

        _tracker.TrackExecution(commandText);
    }
}
