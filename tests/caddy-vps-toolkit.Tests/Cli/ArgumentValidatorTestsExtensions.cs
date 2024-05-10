#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using CaddyVpsToolkit.Cli;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Cli
{
    public static class ArgumentValidatorTestsExtensions
    {
        /// <summary>
        /// Creates a command descriptor with the specified required arguments.
        /// </summary>
        /// <param name="requiredArgs">The required arguments to include in the descriptor.</param>
        /// <returns>A new <see cref="CommandDescriptor"/> instance with the specified required arguments.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="requiredArgs"/> is null.</exception>
        public static CommandDescriptor WithRequiredArgs(this CommandDescriptor descriptor, params string[] requiredArgs)
        {
            ArgumentNullException.ThrowIfNull(requiredArgs);
            ArgumentNullException.ThrowIfNull(descriptor);

            foreach (var arg in requiredArgs)
            {
                descriptor.RequireArgument(arg);
            }

            return descriptor;
        }

        /// <summary>
        /// Creates a command descriptor with the specified optional flags.
        /// </summary>
        /// <param name="optionalFlags">The optional flags to include in the descriptor.</param>
        /// <returns>A new <see cref="CommandDescriptor"/> instance with the specified optional flags.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionalFlags"/> is null.</exception>
        public static CommandDescriptor WithOptionalFlags(this CommandDescriptor descriptor, params string[] optionalFlags)
        {
            ArgumentNullException.ThrowIfNull(optionalFlags);
            ArgumentNullException.ThrowIfNull(descriptor);

            foreach (var flag in optionalFlags)
            {
                descriptor.AllowFlag(flag);
            }

            return descriptor;
        }

        /// <summary>
        /// Validates that the validator correctly identifies missing required arguments.
        /// </summary>
        /// <param name="args">The command line arguments to test.</param>
        /// <param name="requiredArgs">The required arguments that should be detected as missing.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="args"/> or <paramref name="requiredArgs"/> is null.</exception>
        public static void ShouldDetectMissingRequiredArguments(this ArgumentValidator validator, string[] args, params string[] requiredArgs)
        {
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(requiredArgs);

            var descriptor = new CommandDescriptor("test", "Test command")
                .WithRequiredArgs(requiredArgs);

            var parser = new ArgumentParser(args);
            var result = validator.Validate(parser, descriptor);

            result.IsValid.Should().BeFalse("Missing required arguments should make validation fail");
            result.Errors.Should().Contain(e => requiredArgs.Any(arg => e.Contains(arg)));
        }

        /// <summary>
        /// Validates that the validator correctly identifies unknown flags.
        /// </summary>
        /// <param name="args">The command line arguments to test.</param>
        /// <param name="allowedFlags">The allowed flags that should not trigger validation errors.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="args"/> or <paramref name="allowedFlags"/> is null.</exception>
        public static void ShouldDetectUnknownFlags(this ArgumentValidator validator, string[] args, params string[] allowedFlags)
        {
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(allowedFlags);

            var descriptor = new CommandDescriptor("test", "Test command")
                .WithOptionalFlags(allowedFlags);

            var parser = new ArgumentParser(args);
            var result = validator.Validate(parser, descriptor);

            result.IsValid.Should().BeFalse("Unknown flags should make validation fail");
            result.Errors.Should().Contain(e => e.StartsWith("Unknown flag:"));
        }
    }
}