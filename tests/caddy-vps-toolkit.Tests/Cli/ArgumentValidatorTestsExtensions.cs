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
        /// Creates a validator with a command descriptor that has the specified required arguments.
        /// </summary>
        public static ArgumentValidator CreateValidatorWithRequiredArgs(this ArgumentValidator validator, params string[] requiredArgs)
        {
            var descriptor = new CommandDescriptor("test", "Test command")
            {
                RequiredArguments = new System.Collections.Generic.List<string>(requiredArgs),
                OptionalFlags = new System.Collections.Generic.List<string>()
            };

            var parser = new ArgumentParser(new[] { "test" });
            var result = validator.Validate(parser, descriptor);

            return validator;
        }

        /// <summary>
        /// Creates a validator with a command descriptor that has the specified optional flags.
        /// </summary>
        public static ArgumentValidator CreateValidatorWithOptionalFlags(this ArgumentValidator validator, params string[] optionalFlags)
        {
            var descriptor = new CommandDescriptor("test", "Test command")
            {
                RequiredArguments = new System.Collections.Generic.List<string>(),
                OptionalFlags = new System.Collections.Generic.List<string>(optionalFlags)
            };

            var parser = new ArgumentParser(new[] { "test" });
            var result = validator.Validate(parser, descriptor);

            return validator;
        }

        /// <summary>
        /// Validates that the validator correctly identifies missing required arguments.
        /// </summary>
        public static void ShouldDetectMissingRequiredArguments(this ArgumentValidator validator, string[] args, params string[] requiredArgs)
        {
            var descriptor = new CommandDescriptor("test", "Test command")
            {
                RequiredArguments = new System.Collections.Generic.List<string>(requiredArgs),
                OptionalFlags = new System.Collections.Generic.List<string>()
            };

            var parser = new ArgumentParser(args);
            var result = validator.Validate(parser, descriptor);

            result.IsValid.Should().BeFalse("Missing required arguments should make validation fail");
            result.Errors.Should().Contain(e => requiredArgs.Any(arg => e.Contains(arg)));
        }

        /// <summary>
        /// Validates that the validator correctly identifies unknown flags.
        /// </summary>
        public static void ShouldDetectUnknownFlags(this ArgumentValidator validator, string[] args, params string[] allowedFlags)
        {
            var descriptor = new CommandDescriptor("test", "Test command")
            {
                RequiredArguments = new System.Collections.Generic.List<string>(),
                OptionalFlags = new System.Collections.Generic.List<string>(allowedFlags)
            };

            var parser = new ArgumentParser(args);
            var result = validator.Validate(parser, descriptor);

            result.IsValid.Should().BeFalse("Unknown flags should make validation fail");
            result.Errors.Should().Contain(e => e.StartsWith("Unknown flag:"));
        }
    }
}