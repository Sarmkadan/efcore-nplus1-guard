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
    /// <value>The threshold. Default: 5</value>
    public int Threshold { get; set; } = 5;

    /// <summary>
    /// The time window for detecting N+1 queries.
    /// </summary>
    /// <value>The detection window. Default: 00:00:02</value>
    public TimeSpan DetectionWindow { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Whether to throw an exception when an N+1 query is detected.
    /// </summary>
    /// <value><c>true</c> if throw on detection; otherwise, <c>false</c>. Default: false</value>
    public bool ThrowOnDetection { get; set; } = false;

    /// <summary>
    /// Whether to log an N+1 query detection.
    /// </summary>
    /// <value><c>true</c> if log on detection; otherwise, <c>false</c>. Default: true</value>
    public bool LogOnDetection { get; set; } = true;

    /// <summary>
    /// Patterns of queries to ignore.
    /// </summary>
    /// <value>The ignored query patterns. Default: an empty list.</value>
    public List<string> IgnoredQueryPatterns { get; set; } = new List<string>();

    /// <summary>
    /// The repeat count at which severity becomes <c>Medium</c>.
    /// Incidents with a repeat count < this value are considered <c>Low</c>.
    /// </summary>
    /// <value>The repeat count at which severity becomes <c>Medium</c>. Incidents with a repeat count < this value are considered <c>Low</c>. Default: 10</value>
    public int LowSeverityThreshold { get; set; } = 10;

    /// <summary>
    /// The repeat count at which severity becomes <c>High</c>.
    /// Incidents with a repeat count >= this value are considered <c>High</c>.
    /// </summary>
    /// <value>The repeat count at which severity becomes <c>High</c>. Incidents with a repeat count >= this value are considered <c>High</c>. Default: 50</value>
    public int MediumSeverityThreshold { get; set; } = 50;

    /// <summary>
    /// Whether to capture the call site (method name, file name, line number) for N+1 incidents.
    /// When enabled, the first application frame (excluding EF Core, System, and this library's frames)
    /// is captured and stored in the CallSite property.
    /// </summary>
    /// <remarks>
    /// Retained for backward compatibility as a thin bridge onto <see cref="CaptureCallSites"/>:
    /// reading it returns <see langword="true"/> unless <see cref="CaptureCallSites"/> is
    /// <see cref="CaptureCallSiteMode.Never"/>; writing it maps <see langword="false"/> to
    /// <see cref="CaptureCallSiteMode.Never"/> and <see langword="true"/> to
    /// <see cref="CaptureCallSiteMode.OnIncidentOnly"/>.
    /// </remarks>
    /// <value><c>true</c> to capture call site; otherwise, <c>false</c>. Default: true</value>
    [Obsolete("Use CaptureCallSites for finer-grained control over when call sites are captured.")]
    public bool CaptureCallSite
    {
        get => CaptureCallSites != CaptureCallSiteMode.Never;
        set => CaptureCallSites = value ? CaptureCallSiteMode.OnIncidentOnly : CaptureCallSiteMode.Never;
    }

    /// <summary>
    /// Controls when the (expensive) call site is captured via a stack walk. Defaults to
    /// <see cref="CaptureCallSiteMode.OnIncidentOnly"/> so the per-query hot path never pays the
    /// cost of a stack walk unless an N+1 incident has actually been detected.
    /// </summary>
    /// <value>The call site capture mode. Default: OnIncidentOnly</value>
    public CaptureCallSiteMode CaptureCallSites { get; set; } = CaptureCallSiteMode.OnIncidentOnly;

    /// <summary>
    /// The maximum number of stack frames to capture when resolving call sites.
    /// </summary>
    /// <value>The maximum number of stack frames to capture when resolving call sites. Default: int.MaxValue</value>
    public int MaxStackFrames { get; set; } = int.MaxValue;

    /// <summary>
    /// Maximum allowed length for SQL command text in bytes. Queries exceeding this limit
    /// will be truncated to prevent excessive memory allocation during normalization and hashing.
    /// Defaults to 8KB (8192 bytes), which is sufficient for most SQL queries while preventing
    /// denial-of-service via extremely large queries.
    /// </summary>
    /// <value>Maximum allowed length for SQL command text in bytes. Default: 8192</value>
    public int MaxSqlLength { get; set; } = 8192;

    /// <summary>
    /// Maximum allowed length for call site information in characters. Call sites exceeding this limit
    /// will be truncated to prevent excessive memory allocation during fingerprint creation.
    /// Defaults to 4KB (4096 characters), which is sufficient for most stack traces while preventing
    /// denial-of-service via extremely large call site strings.
    /// </summary>
    /// <value>Maximum allowed length for call site information in characters. Default: 4096</value>
    public int MaxCallSiteLength { get; set; } = 4096;

    /// <summary>
    /// Controls how file-based reporters behave when they exhaust their write retries
    /// (e.g. the file is locked by a log shipper or antivirus, permissions were revoked,
    /// or the disk is full). Defaults to <see cref="EfCoreNPlusOneGuard.ReporterFailureMode.LogOnce"/>
    /// so a reporter outage never brings down the observed application.
    /// </summary>
    /// <value>The reporter failure degradation mode. Default: LogOnce</value>
    public ReporterFailureMode ReporterFailureMode { get; set; } = ReporterFailureMode.LogOnce;

    /// <summary>
    /// The minimum interval between rate-limited warning log entries emitted when a
    /// reporter drops incidents because of persistent write failures. Only applies when
    /// <see cref="ReporterFailureMode"/> is <see cref="EfCoreNPlusOneGuard.ReporterFailureMode.LogOnce"/>.
    /// </summary>
    /// <value>The minimum time between warning log entries. Defaults to 5 minutes.</value>
    public TimeSpan ReporterFailureLogInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// A call-site whitelist that suppresses N+1 incidents matching one of its exact,
    /// wildcard-pattern, or query-fingerprint entries. Defaults to <see langword="null"/>
    /// (no suppression). Assign a <see cref="EfCoreNPlusOneGuard.CallSiteWhitelistFileProvider"/>'s
    /// <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}.CurrentValue"/> here, or
    /// swap in a fresh <see cref="EfCoreNPlusOneGuard.CallSiteWhitelist"/> whenever hot-reloading
    /// from a file, to pick up whitelist edits without restarting the observed application.
    /// </summary>
    /// <value>The active call-site whitelist, or <see langword="null"/> if none is configured. Default: null</value>
    public CallSiteWhitelist? CallSiteWhitelist { get; set; }
}

/// <summary>
/// Controls when the guard captures a call site (stack walk) for a tracked query.
/// </summary>
public enum CaptureCallSiteMode
{
    /// <summary>
    /// Never capture a call site. Cheapest option; incidents will have no <c>CallSite</c> value.
    /// </summary>
    Never,

    /// <summary>
    /// Capture a call site only after an N+1 incident has been detected (default). This keeps the
    /// per-query hot path free of stack-walk costs while still giving actionable incident reports.
    /// </summary>
    OnIncidentOnly,

    /// <summary>
    /// Always capture a call site for every tracked query. Expensive; intended for diagnostics only.
    /// </summary>
    Always
}