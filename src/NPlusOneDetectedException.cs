// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using System;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Exception thrown when an N+1 query pattern is detected.
/// </summary>
public class NPlusOneDetectedException : Exception
{
    /// <summary>
    /// The incident details.
    /// </summary>
    public NPlusOneIncident Incident { get; }

    /// <summary>
    /// Creates a new <see cref="NPlusOneDetectedException"/> instance.
    /// </summary>
    /// <param name="incident">The incident details.</param>
    public NPlusOneDetectedException(NPlusOneIncident incident)
        : base(CreateMessage(incident))
    {
        Incident = incident ?? throw new ArgumentNullException(nameof(incident));
    }

    private static string CreateMessage(NPlusOneIncident incident)
    {
        return $"N+1 query pattern detected. Query executed {incident.Count} times.";
    }
}
