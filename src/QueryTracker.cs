// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Concurrent;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Tracks query executions and detects N+1 patterns.
/// </summary>
internal class QueryTracker
{
    private readonly NPlusOneGuardOptions _options;
    private readonly Action<NPlusOneIncident>? _onDetected;
    private readonly ConcurrentDictionary<string, int> _executionCounts = new();

    public QueryTracker(NPlusOneGuardOptions options, Action<NPlusOneIncident>? onDetected = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _onDetected = onDetected;
    }

    public void TrackExecution(string commandText)
    {
        if (commandText is null)
        {
            throw new ArgumentNullException(nameof(commandText));
        }

        _executionCounts.AddOrUpdate(commandText, 1, (_, count) => count + 1);
        CheckForNPlusOnePattern();
    }

    private void CheckForNPlusOnePattern()
    {
        var threshold = _options.Threshold;

        if (threshold <= 1)
        {
            return;
        }

        foreach (var kvp in _executionCounts)
        {
            if (kvp.Value >= threshold)
            {
                ReportIncident(kvp.Key, kvp.Value);
            }
        }
    }

    private void ReportIncident(string commandText, int executionCount)
    {
        var callStack = Environment.StackTrace;
        var incident = new NPlusOneIncident
        {
            SqlQuery = commandText,
            Count = executionCount,
            Severity = NPlusOneSeverity.High,
            StackTrace = callStack
        };

        if (_options.ThrowOnDetection)
        {
            throw new NPlusOneDetectedException(incident);
        }

        if (_options.LogOnDetection)
        {
            Console.Error.WriteLine($"[N+1 Guard] N+1 query pattern detected. Query executed {executionCount} times.");
            Console.Error.WriteLine($"[N+1 Guard] SQL: {commandText.Substring(0, Math.Min(100, commandText.Length))}...");
        }

        _onDetected?.Invoke(incident);
    }

    public void Reset()
    {
        _executionCounts.Clear();
    }
}
