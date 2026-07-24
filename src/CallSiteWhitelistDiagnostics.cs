// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using Microsoft.Extensions.Logging;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Emits startup diagnostics for a <see cref="CallSiteWhitelist"/>, warning about entries that
/// are already expired and entries that have never matched anything, so dead suppressions can
/// be found and cleaned up.
/// </summary>
public static class CallSiteWhitelistDiagnostics
{
    /// <summary>
    /// Logs a warning for each already-expired entry and a warning for each entry that has never
    /// produced a match. Intended to be called once at application startup, after any warm-up
    /// traffic has had a chance to exercise the whitelist.
    /// </summary>
    /// <param name="whitelist">The whitelist to inspect.</param>
    /// <param name="logger">The logger to write diagnostics to.</param>
    /// <returns>The total number of expired-or-unmatched entries reported.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="whitelist"/> or <paramref name="logger"/> is null.</exception>
    public static int LogStaleEntries(this CallSiteWhitelist whitelist, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(whitelist);
        ArgumentNullException.ThrowIfNull(logger);

        var now = DateTimeOffset.UtcNow;
        var expired = whitelist.GetExpiredEntries(now);
        var neverMatched = whitelist.GetNeverMatchedEntries();

        foreach (var entry in expired)
        {
            logger.LogWarning(
                "N+1 guard whitelist entry '{Entry}' has expired and is no longer suppressing incidents; remove it from the whitelist.",
                entry);
        }

        foreach (var entry in neverMatched)
        {
            logger.LogWarning(
                "N+1 guard whitelist entry '{Entry}' has never matched an incident; consider removing it as a dead suppression.",
                entry);
        }

        return expired.Count + neverMatched.Count;
    }
}
