using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides extension methods for <see cref="NPlusOneGuardOptions"/>.
    /// </summary>
    public static class NPlusOneGuardOptionsExtensions
    {
        /// <summary>
        /// Checks if the given query pattern is ignored by the options.
        /// </summary>
        /// <param name="options">The options to check.</param>
        /// <param name="queryPattern">The query pattern to check.</param>
        /// <returns>True if the query pattern is ignored, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="queryPattern"/> is null or empty.</exception>
        public static bool IsQueryIgnored(this NPlusOneGuardOptions options, string queryPattern)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrEmpty(queryPattern);

            return options.IgnoredQueryPatterns.Any(pattern => queryPattern.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the threshold value as a string representation.
        /// </summary>
        /// <param name="options">The options to get the threshold from.</param>
        /// <returns>A string representation of the threshold value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        public static string GetThresholdString(this NPlusOneGuardOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.Threshold.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the detection window value as a string representation.
        /// </summary>
        /// <param name="options">The options to get the detection window from.</param>
        /// <returns>A string representation of the detection window value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        public static string GetDetectionWindowString(this NPlusOneGuardOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.DetectionWindow.ToString("g", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Checks if the options are configured to throw an exception on detection.
        /// </summary>
        /// <param name="options">The options to check.</param>
        /// <returns>True if the options are configured to throw an exception, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        public static bool IsThrowOnDetectionEnabled(this NPlusOneGuardOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.ThrowOnDetection;
        }

        /// <summary>
        /// Checks if the options are configured to log detections.
        /// </summary>
        /// <param name="options">The options to check.</param>
        /// <returns>True if the options are configured to log detections, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
        public static bool IsLogOnDetectionEnabled(this NPlusOneGuardOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.LogOnDetection;
        }
    }
}