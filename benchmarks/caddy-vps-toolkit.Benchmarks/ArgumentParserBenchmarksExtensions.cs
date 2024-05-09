using System;
using System.Collections.Generic;
using System.Globalization;
using CaddyVpsToolkit.Cli;

namespace CaddyVpsToolkit.Benchmarks
{
    /// <summary>
    /// Extension methods for <see cref="ArgumentParserBenchmarks"/> that provide strongly-typed
    /// access to benchmark results with proper validation and error handling.
    /// </summary>
    public static class ArgumentParserBenchmarksExtensions
    {
        /// <summary>
        /// Determines whether the parsed command matches the specified command name.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance (cannot be null).</param>
        /// <param name="command">The command name to compare against.</param>
        /// <returns><see langword="true"/> if the command matches; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
        public static bool HasGetCommand(this ArgumentParserBenchmarks benchmarks, string command)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentNullException.ThrowIfNull(command);

            return benchmarks.GetCommand_Small() == command;
        }

        /// <summary>
        /// Gets the total count of flags parsed in the benchmark scenario.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance (cannot be null).</param>
        /// <returns>The number of flags parsed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
        public static int GetFlagCount(this ArgumentParserBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            return benchmarks.GetAllFlags_Large().Count;
        }

        /// <summary>
        /// Calculates the average value of flag values parsed in the benchmark scenario.
        /// Handles both equals syntax (--flag=value) and space syntax (--flag value).
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance (cannot be null).</param>
        /// <returns>The average of all parsed flag values, or 0 if no values are present.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">Thrown when a flag value cannot be parsed as a double.</exception>
        public static double GetFlagValueAverage(this ArgumentParserBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            var values = new List<double>();

            string? equalsSyntaxValue = benchmarks.GetFlagValue_EqualsSyntax();
            if (equalsSyntaxValue is not null)
            {
                values.Add(double.Parse(equalsSyntaxValue, CultureInfo.InvariantCulture));
            }

            string? spaceSyntaxValue = benchmarks.GetFlagValue_SpaceSyntax();
            if (spaceSyntaxValue is not null)
            {
                values.Add(double.Parse(spaceSyntaxValue, CultureInfo.InvariantCulture));
            }

            return values.Count > 0 ? values.Average() : 0;
        }

        /// <summary>
        /// Determines whether any flags were parsed in the benchmark scenario.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance (cannot be null).</param>
        /// <returns><see langword="true"/> if any flags are present; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/>.</exception>
        public static bool HasAnyFlags(this ArgumentParserBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            return benchmarks.HasFlag_Present() || benchmarks.GetAllFlags_Large().Count > 0;
        }
    }
}