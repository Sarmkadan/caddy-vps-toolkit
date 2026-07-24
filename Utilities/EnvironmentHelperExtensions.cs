#nullable enable

using System;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Extension helpers for <see cref="EnvironmentHelper"/> providing convenient
    /// typed accessors and required variable checks.
    /// </summary>
    public static class EnvironmentHelperExtensions
    {
        /// <summary>
        /// Retrieves the value of an environment variable and throws if it is missing.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <returns>The variable value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the variable is not set.
        /// </exception>
        public static string GetRequiredVariable(string name)
        {
            var value = EnvironmentHelper.GetEnvironmentVariable(name);
            if (value is null)
            {
                throw new InvalidOperationException(
                    $"Required environment variable '{name}' is not set.");
            }

            return value;
        }

        /// <summary>
        /// Retrieves the value of an environment variable as a boolean.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="defaultValue">The value to return if the variable is not set.</param>
        /// <returns>The parsed boolean value or the default.</returns>
        /// <exception cref="FormatException">
        /// Thrown when the variable is set but cannot be parsed as a boolean.
        /// </exception>
        public static bool GetBool(string name, bool defaultValue = false)
        {
            var value = EnvironmentHelper.GetEnvironmentVariable(name);
            if (value is null)
            {
                return defaultValue;
            }

            if (bool.TryParse(value, out var result))
            {
                return result;
            }

            throw new FormatException(
                $"Environment variable '{name}' value '{value}' is not a valid boolean.");
        }

        /// <summary>
        /// Retrieves the value of an environment variable as an integer.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="defaultValue">The value to return if the variable is not set.</param>
        /// <returns>The parsed integer value or the default.</returns>
        /// <exception cref="FormatException">
        /// Thrown when the variable is set but cannot be parsed as an integer.
        /// </exception>
        public static int GetInt(string name, int defaultValue = 0)
        {
            var value = EnvironmentHelper.GetEnvironmentVariable(name);
            if (value is null)
            {
                return defaultValue;
            }

            if (int.TryParse(value, out var result))
            {
                return result;
            }

            throw new FormatException(
                $"Environment variable '{name}' value '{value}' is not a valid integer.");
        }
    }
}
