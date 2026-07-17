#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CaddyVpsToolkit.Cli
{
    /// <summary>
    /// Extension methods for <see cref="ArgumentParser"/> that provide additional parsing conveniences.
    /// </summary>
    public static class ArgumentParserExtensions
    {
        /// <summary>
        /// Gets the command name as a span for zero-allocation scenarios.
        /// </summary>
        /// <param name="parser">The argument parser instance.</param>
        /// <returns>A span containing the command name, or empty span if no command.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is <see langword="null"/>.</exception>
        public static ReadOnlySpan<char> GetCommandSpan(this ArgumentParser parser)
        {
            ArgumentNullException.ThrowIfNull(parser);
            return parser.GetCommand().AsSpan();
        }

        /// <summary>
        /// Gets a positional argument and parses it as an integer.
        /// </summary>
        /// <param name="parser">The argument parser instance.</param>
        /// <param name="index">The 0-based index of the positional argument.</param>
        /// <returns>The parsed integer value, or <see langword="null"/> if the argument is not present or not a valid integer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
        public static int? GetPositionalAsInt(this ArgumentParser parser, int index)
        {
            ArgumentNullException.ThrowIfNull(parser);
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            var value = parser.GetPositional(index);
            if (value is null || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Gets a positional argument and parses it as a boolean flag value.
        /// Treats "true", "1", "yes", "on" as true; "false", "0", "no", "off" as false; anything else as null.
        /// </summary>
        /// <param name="parser">The argument parser instance.</param>
        /// <param name="index">The 0-based index of the positional argument.</param>
        /// <returns>The parsed boolean value, or <see langword="null"/> if the argument is not present or not a valid boolean string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
        public static bool? GetPositionalAsBoolean(this ArgumentParser parser, int index)
        {
            ArgumentNullException.ThrowIfNull(parser);
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            var value = parser.GetPositional(index);
            if (value is null)
            {
                return null;
            }

            return value.ToLowerInvariant() switch
            {
                "true" or "1" or "yes" or "on" => true,
                "false" or "0" or "no" or "off" => false,
                _ => null
            };
        }

        /// <summary>
        /// Gets a flag value and parses it as an integer.
        /// </summary>
        /// <param name="parser">The argument parser instance.</param>
        /// <param name="flagName">The name of the flag to check.</param>
        /// <returns>The parsed integer value, or <see langword="null"/> if the flag is not present or its value is not a valid integer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="flagName"/> is <see langword="null"/> or empty.</exception>
        public static int? GetFlagValueAsInt(this ArgumentParser parser, string flagName)
        {
            ArgumentNullException.ThrowIfNull(parser);
            ArgumentException.ThrowIfNullOrEmpty(flagName);

            var value = parser.GetFlagValue(flagName);
            if (value is null || value.Length == 0)
            {
                return null;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Gets a flag value and parses it as a boolean.
        /// Treats "true", "1", "yes", "on" as true; "false", "0", "no", "off" as false; anything else as null.
        /// </summary>
        /// <param name="parser">The argument parser instance.</param>
        /// <param name="flagName">The name of the flag to check.</param>
        /// <returns>The parsed boolean value, or <see langword="null"/> if the flag is not present or its value is not a valid boolean string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="flagName"/> is <see langword="null"/> or empty.</exception>
        public static bool? GetFlagValueAsBoolean(this ArgumentParser parser, string flagName)
        {
            ArgumentNullException.ThrowIfNull(parser);
            ArgumentException.ThrowIfNullOrEmpty(flagName);

            var value = parser.GetFlagValue(flagName);
            if (value is null || value.Length == 0)
            {
                return null;
            }

            return value.ToLowerInvariant() switch
            {
                "true" or "1" or "yes" or "on" => true,
                "false" or "0" or "no" or "off" => false,
                _ => null
            };
        }

        /// <summary>
        /// Determines whether a specific flag is present and has a non-empty value.
        /// </summary>
        /// <param name="parser">The argument parser instance.</param>
        /// <param name="flagName">The name of the flag to check.</param>
        /// <returns><see langword="true"/> if the flag is present and has a non-empty value; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="flagName"/> is <see langword="null"/> or empty.</exception>
        public static bool HasFlagWithValue(this ArgumentParser parser, string flagName)
        {
            ArgumentNullException.ThrowIfNull(parser);
            ArgumentException.ThrowIfNullOrEmpty(flagName);

            return parser.HasFlag(flagName) && parser.GetFlagValue(flagName) is not null;
        }

        /// <summary>
        /// Gets all positional arguments as a read-only list.
        /// </summary>
        /// <param name="parser">The argument parser instance.</param>
        /// <returns>A read-only list of all positional arguments.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> GetAllPositionalReadOnly(this ArgumentParser parser)
        {
            ArgumentNullException.ThrowIfNull(parser);
            return parser.GetAllPositional().AsReadOnly();
        }

        /// <summary>
        /// Gets all flags as a read-only list.
        /// </summary>
        /// <param name="parser">The argument parser instance.</param>
        /// <returns>A read-only list of all flag names.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> GetAllFlagsReadOnly(this ArgumentParser parser)
        {
            ArgumentNullException.ThrowIfNull(parser);
            return parser.GetAllFlags().AsReadOnly();
        }

        /// <summary>
        /// Gets the number of positional arguments available.
        /// </summary>
        /// <param name="parser">The argument parser instance.</param>
        /// <returns>The count of positional arguments.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is <see langword="null"/>.</exception>
        public static int GetPositionalCount(this ArgumentParser parser)
        {
            ArgumentNullException.ThrowIfNull(parser);
            return parser.GetAllPositional().Count;
        }

        /// <summary>
        /// Determines whether any of the specified flags are present.
        /// </summary>
        /// <param name="parser">The argument parser instance.</param>
        /// <param name="flagNames">The flag names to check.</param>
        /// <returns><see langword="true"/> if any of the specified flags are present; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> or <paramref name="flagNames"/> is <see langword="null"/>.</exception>
        public static bool HasAnyFlag(this ArgumentParser parser, params string[] flagNames)
        {
            ArgumentNullException.ThrowIfNull(parser);
            ArgumentNullException.ThrowIfNull(flagNames);

            foreach (var flagName in flagNames)
            {
                if (parser.HasFlag(flagName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}