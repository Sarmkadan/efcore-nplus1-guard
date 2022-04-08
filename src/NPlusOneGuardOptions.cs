// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable
using System;

namespace EfCoreNPlusOneGuard
{
    public class NPlusOneGuardOptions
    {
        public int Threshold { get; set; } = 5;
        public TimeSpan DetectionWindow { get; set; } = TimeSpan.FromSeconds(2);
        public bool ThrowOnDetection { get; set; } = false;
        public bool LogOnDetection { get; set; } = true;
        public List<string> IgnoredQueryPatterns { get; set; } = new List<string>();
    }
}

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
}
