// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Options for the N+1 Guard.
/// </summary>
public class NPlusOneGuardOptions
{
    /// <summary>
    /// The threshold for detecting N+1 queries.
    /// </summary>
    /// <value>The threshold.</value>
    public int Threshold { get; set; } = 5;

    /// <summary>
    /// The time window for detecting N+1 queries.
    /// </summary>
    /// <value>The detection window.</value>
    public TimeSpan DetectionWindow { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Whether to throw an exception when an N+1 query is detected.
    /// </summary>
    /// <value><c>true</c> if throw on detection; otherwise, <c>false</c>.</value>
    public bool ThrowOnDetection { get; set; } = false;

    /// <summary>
    /// Whether to log an N+1 query detection.
    /// </summary>
    /// <value><c>true</c> if log on detection; otherwise, <c>false</c>.</value>
    public bool LogOnDetection { get; set; } = true;

    /// <summary>
    /// Patterns of queries to ignore.
    /// </summary>
    /// <value>The ignored query patterns.</value>
    public List<string> IgnoredQueryPatterns { get; set; } = new List<string>();

    /// <summary>
    /// The repeat count at which severity becomes <c>Medium</c>.
    /// Incidents with a repeat count &lt; this value are considered <c>Low</c>.
    /// </summary>
    public int LowSeverityThreshold { get; set; } = 10;

    /// <summary>
    /// The repeat count at which severity becomes <c>High</c>.
    /// Incidents with a repeat count &gt;= this value are considered <c>High</c>.
    /// </summary>
    public int MediumSeverityThreshold { get; set; } = 50;
}
