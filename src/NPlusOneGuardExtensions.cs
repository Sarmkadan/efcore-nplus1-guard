// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Extension methods for configuring N+1 query detection in EF Core.
/// </summary>
public static class NPlusOneGuardExtensions
{
    /// <summary>
    /// Configures and enables N+1 query detection for the DbContext.
    /// </summary>
    /// <param name="builder">The DbContextOptionsBuilder to configure.</param>
    /// <param name="configure">Optional action to configure NPlusOneGuardOptions.</param>
    /// <param name="onDetected">Optional callback invoked when N+1 query is detected.</param>
    /// <returns>The configured DbContextOptionsBuilder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static DbContextOptionsBuilder UseNPlusOneGuard(
        this DbContextOptionsBuilder builder,
        Action<NPlusOneGuardOptions>? configure = null,
        Action<NPlusOneIncident>? onDetected = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new NPlusOneGuardOptions();
        configure?.Invoke(options);

        var interceptor = new NPlusOneGuardInterceptor(options, onDetected);
        builder.AddInterceptors(interceptor);

        return builder;
    }
}
