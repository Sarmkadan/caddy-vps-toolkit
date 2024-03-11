#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using CaddyVpsToolkit.Domain.Models;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Services
{
    /// <summary>
    /// Extension methods for <see cref="SystemdUnitConfigTests"/> providing reusable test assertions and helpers.
    /// </summary>
    public static class SystemdUnitConfigTestsExtensions
    {
        /// <summary>
        /// Validates that the configuration is valid by ensuring no exception is thrown.
        /// </summary>
        /// <param name="test">The test instance.</param>
        /// <param name="config">The configuration to validate.</param>
        public static void Validate_ShouldNotThrow(this SystemdUnitConfigTests test, SystemdUnitConfig config)
        {
            test.Invoking(_ => config.Validate()).Should().NotThrow();
        }

        /// <summary>
        /// Validates that the configuration throws a validation exception with the expected message pattern.
        /// </summary>
        /// <param name="test">The test instance.</param>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="expectedMessagePattern">The expected message pattern (e.g., "*Unit name*").</param>
        public static void Validate_ShouldThrowWithMessage(this SystemdUnitConfigTests test, SystemdUnitConfig config, string expectedMessagePattern)
        {
            config.Invoking(c => c.Validate())
                .Should()
                .Throw<System.ComponentModel.DataAnnotations.ValidationException>()
                .WithMessage(expectedMessagePattern);
        }

        /// <summary>
        /// Generates systemd content and asserts it contains the expected directives.
        /// </summary>
        /// <param name="test">The test instance.</param>
        /// <param name="config">The configuration to generate content from.</param>
        /// <param name="expectedDirectives">Collection of directive patterns that must be present.</param>
        /// <param name="unexpectedDirectives">Collection of directive patterns that must NOT be present.</param>
        public static void GenerateSystemdContent_ShouldContainDirectives(
            this SystemdUnitConfigTests test,
            SystemdUnitConfig config,
            IEnumerable<string> expectedDirectives,
            IEnumerable<string>? unexpectedDirectives = null)
        {
            var content = config.GenerateSystemdContent();

            foreach (var directive in expectedDirectives)
            {
                content.Should().Contain(directive);
            }

            if (unexpectedDirectives != null)
            {
                foreach (var directive in unexpectedDirectives)
                {
                    content.Should().NotContain(directive);
                }
            }
        }

        /// <summary>
        /// Generates systemd content and asserts the section order is correct.
        /// </summary>
        /// <param name="test">The test instance.</param>
        /// <param name="config">The configuration to generate content from.</param>
        /// <param name="expectedUnitSection">Expected content for the [Unit] section.</param>
        /// <param name="expectedServiceSection">Expected content for the [Service] section.</param>
        /// <param name="expectedInstallSection">Expected content for the [Install] section.</param>
        public static void GenerateSystemdContent_ShouldHaveCorrectStructure(
            this SystemdUnitConfigTests test,
            SystemdUnitConfig config,
            string expectedUnitSection,
            string expectedServiceSection,
            string expectedInstallSection)
        {
            var content = config.GenerateSystemdContent();

            var unitIdx = content.IndexOf("[Unit]", StringComparison.Ordinal);
            var serviceIdx = content.IndexOf("[Service]", StringComparison.Ordinal);
            var installIdx = content.IndexOf("[Install]", StringComparison.Ordinal);

            unitIdx.Should().BeLessThan(serviceIdx);
            serviceIdx.Should().BeLessThan(installIdx);

            content.Should().Contain(expectedUnitSection);
            content.Should().Contain(expectedServiceSection);
            content.Should().Contain(expectedInstallSection);
        }
    }
}