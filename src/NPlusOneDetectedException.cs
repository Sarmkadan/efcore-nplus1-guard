// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using System;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// The exception that is thrown when an N+1 query pattern is detected.
/// </summary>
public sealed class NPlusOneDetectedException : InvalidOperationException
{
    /// <summary>
    /// Gets the incident details that caused this exception.
    /// </summary>
    public NPlusOneIncident Incident { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NPlusOneDetectedException"/> class.
    /// </summary>
    /// <param name="incident">The incident details that caused this exception.</param>
    public NPlusOneDetectedException(NPlusOneIncident incident)
        : base(incident.ToString())
    {
        Incident = incident;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NPlusOneDetectedException"/> class with a
    /// custom message, e.g. one that aggregates several incidents for a test-assertion failure.
    /// </summary>
    /// <param name="incident">The representative incident that caused this exception.</param>
    /// <param name="message">The exception message.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="incident"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is <see langword="null"/> or empty.</exception>
    public NPlusOneDetectedException(NPlusOneIncident incident, string message)
        : base(message)
    {
        ArgumentNullException.ThrowIfNull(incident);
        ArgumentException.ThrowIfNullOrEmpty(message);

        Incident = incident;
    }
}