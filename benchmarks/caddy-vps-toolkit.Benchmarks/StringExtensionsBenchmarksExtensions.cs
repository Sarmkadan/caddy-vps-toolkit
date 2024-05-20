using System;
using System.Text;

namespace CaddyVpsToolkit.Benchmarks
{
    /// <summary>
    /// Provides benchmark-oriented extension methods for <see cref="string"/> that are exercised in the
    /// performance tests of the CaddyVpsToolkit project.
    /// </summary>
    public static class StringExtensionsBenchmarksExtensions
    {
        /// <summary>
        /// Determines whether the specified string ends with any of the provided suffixes using ordinal,
        /// case‑insensitive comparison.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <param name="suffixes">The array of suffixes to compare against.</param>
        /// <returns><see langword="true"/> if the string ends with any suffix; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="suffixes"/> is <see langword="null"/></exception>
        public static bool EndsWithAny(this string value, string[] suffixes)
        {
            ArgumentNullException.ThrowIfNull(suffixes);

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (var suffix in suffixes)
            {
                if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes diacritical marks (accents) from the string.
        /// </summary>
        /// <param name="value">The string to process.</param>
        /// <returns>A new string with diacritical marks removed, or the original string if it was null or empty.</returns>
        public static string RemoveDiacritics(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var character in normalized)
            {
                if (character is >= '̀' and <= 'ͯ')
                {
                    continue;
                }

                builder.Append(character);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Encodes the string as a Base64 URL‑safe string without padding characters.
        /// </summary>
        /// <param name="value">The string to encode.</param>
        /// <returns>The Base64 URL‑safe encoded string, or <see langword="null"/> if the input was null.</returns>
        public static string ToBase64UrlEncode(this string value)
        {
            if (value is null)
            {
                return null;
            }

            if (value.Length == 0)
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
