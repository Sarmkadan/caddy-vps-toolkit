#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using CaddyVpsToolkit.Configuration;
using FluentAssertions;

namespace CaddyVpsToolkit.Tests.Configuration
{
    /// <summary>
    /// Extension methods for <see cref="AppConfigurationBuilderTests"/> that provide convenient testing utilities
    /// for building test configurations with common patterns and assertions.
    /// </summary>
    public static class AppConfigurationBuilderTestsExtensions
    {
        /// <summary>
        /// Creates a new AppConfigurationBuilder with common test settings pre-configured.
        /// </summary>
        /// <param name="builder">The builder instance (can be null for first call)</param>
        /// <param name="settings">Collection of key-value pairs to add as settings</param>
        /// <returns>A configured AppConfigurationBuilder</returns>
        /// <exception cref="ArgumentNullException">Thrown when settings is null</exception>
        public static AppConfigurationBuilder WithTestSettings(this AppConfigurationBuilder? builder, IDictionary<string, object> settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            builder ??= new AppConfigurationBuilder();

            foreach (var (key, value) in settings)
            {
                builder.WithSetting(key, value.ToString()!);
            }

            return builder;
        }

        /// <summary>
        /// Creates a new AppConfigurationBuilder with common web server test settings.
        /// </summary>
        /// <param name="builder">The builder instance (can be null for first call)</param>
        /// <param name="port">The HTTP port to configure</param>
        /// <param name="httpsPort">The HTTPS port to configure (optional)</param>
        /// <param name="enableLogging">Whether to enable verbose logging</param>
        /// <returns>A configured AppConfigurationBuilder</returns>
        public static AppConfigurationBuilder WithWebServerSettings(this AppConfigurationBuilder? builder, int port, int? httpsPort = null, bool enableLogging = false)
        {
            builder ??= new AppConfigurationBuilder();

            builder.WithSetting("Server:Port", port.ToString(CultureInfo.InvariantCulture));
            builder.WithSetting("Server:Host", "localhost");

            if (httpsPort.HasValue)
            {
                builder.WithSetting("Server:HttpsPort", httpsPort.Value.ToString(CultureInfo.InvariantCulture));
            }

            builder.WithSetting("Logging:Enabled", enableLogging.ToString());
            builder.WithSetting("Logging:Level", "Debug");

            return builder;
        }

        /// <summary>
        /// Creates a new AppConfigurationBuilder with common Caddy-specific test settings.
        /// </summary>
        /// <param name="builder">The builder instance (can be null for first call)</param>
        /// <param name="domains">Collection of domains to configure</param>
        /// <param name="enableTls">Whether to enable TLS</param>
        /// <param name="tlsEmail">Email for Let's Encrypt (optional)</param>
        /// <returns>A configured AppConfigurationBuilder</returns>
        /// <exception cref="ArgumentNullException">Thrown when domains is null</exception>
        public static AppConfigurationBuilder WithCaddySettings(this AppConfigurationBuilder? builder, IEnumerable<string> domains, bool enableTls = true, string? tlsEmail = null)
        {
            ArgumentNullException.ThrowIfNull(domains);

            builder ??= new AppConfigurationBuilder();

            builder.WithSetting("Caddy:Enable", "true");
            builder.WithSetting("Caddy:Port", "80");
            builder.WithSetting("Caddy:HttpsPort", "443");

            var domainList = new List<string>(domains);
            for (var i = 0; i < domainList.Count; i++)
            {
                builder.WithSetting($"Caddy:Domains:{i}", domainList[i]);
            }

            if (enableTls)
            {
                builder.WithSetting("Caddy:Tls:Enable", "true");
                if (!string.IsNullOrWhiteSpace(tlsEmail))
                {
                    builder.WithSetting("Caddy:Tls:Email", tlsEmail);
                }
            }

            return builder;
        }

        /// <summary>
        /// Asserts that the configuration contains all expected settings with correct values.
        /// </summary>
        /// <param name="config">The configuration to assert</param>
        /// <param name="expectedSettings">Dictionary of expected key-value pairs</param>
        /// <exception cref="ArgumentNullException">Thrown when config or expectedSettings is null</exception>
        /// <exception cref="ArgumentException">Thrown when a setting is missing or has incorrect value</exception>
        public static void ShouldContainSettings(this AppConfiguration config, IDictionary<string, object> expectedSettings)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(expectedSettings);

            foreach (var (key, expectedValue) in expectedSettings)
            {
                var actualValue = config.GetString(key);
                if (actualValue is null)
                {
                    throw new ArgumentException($"Configuration is missing expected key: {key}");
                }

                if (!string.Equals(actualValue, expectedValue.ToString(), StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Configuration key '{key}' has value '{actualValue}' but expected '{expectedValue}'");
                }
            }
        }

        /// <summary>
        /// Asserts that the configuration contains a setting with a value that can be parsed as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to parse as (int, bool, etc.)</typeparam>
        /// <param name="config">The configuration to assert</param>
        /// <param name="key">The configuration key</param>
        /// <param name="expectedValue">The expected parsed value</param>
        /// <exception cref="ArgumentNullException">Thrown when config or key is null</exception>
        /// <exception cref="ArgumentException">Thrown when the value cannot be parsed or doesn't match expected</exception>
        public static void ShouldHaveValue<T>(this AppConfiguration config, string key, T expectedValue) where T : IConvertible
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(key);

            var stringValue = config.GetString(key);
            if (stringValue is null)
            {
                throw new ArgumentException($"Configuration is missing key: {key}");
            }

            var parsedValue = (T)Convert.ChangeType(stringValue, typeof(T), CultureInfo.InvariantCulture);
            if (!parsedValue.Equals(expectedValue))
            {
                throw new ArgumentException($"Configuration key '{key}' has value '{parsedValue}' but expected '{expectedValue}'");
            }
        }

        /// <summary>
        /// Creates a builder with common test defaults for unit testing.
        /// </summary>
        /// <param name="builder">The builder instance (can be null for first call)</param>
        /// <returns>A configured AppConfigurationBuilder with common test defaults</returns>
        public static AppConfigurationBuilder WithTestDefaults(this AppConfigurationBuilder? builder)
        {
            return builder
                .WithSetting("Test:Environment", "UnitTest")
                .WithSetting("Test:Timeout", "30000")
                .WithSetting("Test:RetryCount", "3")
                .WithSetting("Test:IsolationLevel", "ReadCommitted");
        }
    }
}