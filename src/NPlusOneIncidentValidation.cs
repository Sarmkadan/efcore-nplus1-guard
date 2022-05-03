using System;
using System.Collections.Generic;

namespace EfCoreNPlusOneGuard;

/// <summary>
/// Provides validation helpers for <see cref="NPlusOneIncident"/> instances.
/// </summary>
public static class NPlusOneIncidentValidation
{
    /// <summary>
    /// Validates the <see cref="NPlusOneIncident"/> instance.
    /// </summary>
    /// <param name="value">The incident to validate.</param>
    /// <returns>A read-only list of human-readable problems found in the incident.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this NPlusOneIncident value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.SqlQuery))
        {
            problems.Add("SQL query cannot be null or whitespace.");
        }

        if (value.Count <= 0)
        {
            problems.Add("Execution count must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(value.StackTrace))
        {
            problems.Add("Stack trace cannot be null or whitespace.");
        }

        return problems;
    }

    /// <summary>
    /// Determines whether the <see cref="NPlusOneIncident"/> instance is valid.
    /// </summary>
    /// <param name="value">The incident to check.</param>
    /// <returns><see langword="true"/> if the incident is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this NPlusOneIncident value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures the <see cref="NPlusOneIncident"/> instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The incident to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the incident is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this NPlusOneIncident value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException($"N+1 incident is invalid: {string.Join(", ", problems)}", nameof(value));
        }
    }
}
