using System;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Benchmarks
{
    public static class StringExtensionsBenchmarksExtensions
    {
        public static bool EndsWithAny(this string value, string[] suffixes)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            foreach (var suffix in suffixes)
            {
                if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static string RemoveDiacritics(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new System.Text.StringBuilder();

            foreach (var character in normalized)
            {
                if (character >= 0x0300 && character <= 0x036F) 
                    continue;

                builder.Append(character);
            }

            return builder.ToString();
        }

        public static string ToBase64UrlEncode(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
