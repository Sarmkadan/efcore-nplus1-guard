using System.Collections.Generic;

namespace EfCoreNPlusOneGuard
{
    /// <summary>
    /// Provides validation helpers for the <see cref="DuplicateQueryDetector"/> class.
    /// </summary>
    public static class DuplicateQueryDetectorValidation
    {
        /// <summary>
        /// Validates the provided <see cref="DuplicateQueryDetector"/> instance.
        /// </summary>
        /// <param name="value">The <see cref="DuplicateQueryDetector"/> to validate.</param>
        /// <returns>A list of human-readable validation errors.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this DuplicateQueryDetector value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            foreach (var group in value.GetDuplicates())
            {
                if (string.IsNullOrEmpty(group.Sql))
                {
                    problems.Add($"Duplicate query group has null or empty SQL.");
                }

                if (string.IsNullOrEmpty(group.Parameters))
                {
                    problems.Add($"Duplicate query group has null or empty parameters.");
                }

                if (group.Count <= 0)
                {
                    problems.Add($"Duplicate query group has non-positive count: {group.Count}.");
                }
            }

            return problems;
        }

        /// <summary>
        /// Determines whether the provided <see cref="DuplicateQueryDetector"/> is valid.
        /// </summary>
        /// <param name="value">The <see cref="DuplicateQueryDetector"/> to check.</param>
        /// <returns>True if the detector is valid; otherwise, false.</returns>
        public static bool IsValid(this DuplicateQueryDetector value)
        {
            return Validate(value).Count == 0;
        }

        /// <summary>
        /// Ensures that the provided <see cref="DuplicateQueryDetector"/> is valid.
        /// </summary>
        /// <param name="value">The <see cref="DuplicateQueryDetector"/> to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the detector is invalid.</exception>
        public static void EnsureValid(this DuplicateQueryDetector value)
        {
            var problems = Validate(value);
            if (problems.Count > 0)
            {
                throw new ArgumentException(string.Join("\n", problems), nameof(value));
            }
        }
    }
}
