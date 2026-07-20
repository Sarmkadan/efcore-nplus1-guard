using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Validates <see cref="NPlusOneGuardOptions"/> instances.
    /// </summary>
    public sealed class NPlusOneGuardOptionsValidation : IValidateOptions<NPlusOneGuardOptions>
    {
        /// <summary>
        /// Validates the supplied options.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="options">The options instance to validate.</param>
        /// <returns>A <see cref="ValidateOptionsResult"/> indicating success or failure.</returns>
        public ValidateOptionsResult Validate(string name, NPlusOneGuardOptions options)
        {
            var failures = new List<string>();

            if (options.Threshold <= 0)
            {
                failures.Add("Threshold must be greater than 0.");
            }

            if (options.DetectionWindow <= TimeSpan.Zero)
            {
                failures.Add("DetectionWindow must be greater than zero.");
            }

            if (options.LowSeverityThreshold < 0)
            {
                failures.Add("LowSeverityThreshold must be non-negative.");
            }

            if (options.MediumSeverityThreshold <= options.LowSeverityThreshold)
            {
                failures.Add("MediumSeverityThreshold must be greater than LowSeverityThreshold.");
            }

            if (options.IgnoredQueryPatterns == null)
            {
                failures.Add("IgnoredQueryPatterns cannot be null.");
            }
            else
            {
                foreach (var pattern in options.IgnoredQueryPatterns)
                {
                    if (string.IsNullOrWhiteSpace(pattern))
                    {
                        failures.Add("IgnoredQueryPatterns contains an empty or whitespace pattern.");
                    }
                }
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }
}
