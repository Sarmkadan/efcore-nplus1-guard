// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Threading;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// An <see cref="IIncidentReporter"/> that collects N+1 incidents in-memory instead of writing them
/// to a file or console. Intended for CI/test scenarios where a test wants to assert that no N+1
/// pattern occurred during a piece of code under test.
/// </summary>
/// <remarks>
/// Incidents reported while a reporter is <see cref="Activate">active</see> are also reachable via
/// <see cref="Current"/>, an <see cref="AsyncLocal{T}"/>-backed ambient reporter. Wire your
/// <c>UseNPlusOneGuard</c> <c>onDetected</c> callback to forward incidents to
/// <c>InMemoryIncidentReporter.Current?.Report(incident)</c> so that
/// <c>AssertNoNPlusOne</c> extension methods in <see cref="QueryTrackerExtensions"/> can observe
/// incidents raised on any thread reachable from the calling
/// <see cref="System.Threading.ExecutionContext"/>.
/// </remarks>
public sealed class InMemoryIncidentReporter : IIncidentReporter
{
    private static readonly AsyncLocal<InMemoryIncidentReporter?> _current = new();

    private readonly IncidentAggregator _aggregator = new();

    /// <summary>
    /// Gets the ambient reporter activated by the innermost enclosing
    /// <see cref="Activate"/> scope on the current asynchronous flow, or <see langword="null"/>
    /// if no scope is active.
    /// </summary>
    public static InMemoryIncidentReporter? Current => _current.Value;

    /// <summary>
    /// Gets the aggregator collecting the incidents reported through this instance.
    /// </summary>
    public IncidentAggregator Aggregator => _aggregator;

    /// <summary>
    /// Reports a single incident by adding it to the in-memory aggregator.
    /// </summary>
    /// <param name="incident">The incident to report.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incident"/> is <see langword="null"/>.</exception>
    public void Report(NPlusOneIncident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        _aggregator.Add(incident);
    }

    /// <summary>
    /// Makes <paramref name="reporter"/> the ambient <see cref="Current"/> reporter for the
    /// duration of the returned scope. Nested calls stack correctly: disposing a scope restores
    /// whatever reporter was active before it, on the current asynchronous flow.
    /// </summary>
    /// <param name="reporter">The reporter to activate.</param>
    /// <returns>A disposable that restores the previously active reporter when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reporter"/> is <see langword="null"/>.</exception>
    public static IDisposable Activate(InMemoryIncidentReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(reporter);

        return new ActivationScope(reporter);
    }

    private sealed class ActivationScope : IDisposable
    {
        private readonly InMemoryIncidentReporter? _previous;
        private bool _disposed;

        public ActivationScope(InMemoryIncidentReporter reporter)
        {
            _previous = _current.Value;
            _current.Value = reporter;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _current.Value = _previous;
            _disposed = true;
        }
    }
}
