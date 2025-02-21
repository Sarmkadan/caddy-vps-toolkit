// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.IO;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Helper for working with environment variables and application paths.
    /// Provides safe access to environment data with defaults.
    /// </summary>
    public static class EnvironmentHelper
    {
        /// <summary>
        /// Get environment variable with fallback
        /// </summary>
        public static string GetEnvironmentVariable(string name, string defaultValue = null)
        {
            return Environment.GetEnvironmentVariable(name) ?? defaultValue;
        }

        /// <summary>
        /// Set environment variable
        /// </summary>
        public static void SetEnvironmentVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        /// <summary>
        /// Check if running in development environment
        /// </summary>
        public static bool IsDevelopment()
        {
            var env = GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "production");
            return env.Equals("development", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if running in production environment
        /// </summary>
        public static bool IsProduction()
        {
            var env = GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "production");
            return env.Equals("production", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get application root directory
        /// </summary>
        public static string GetApplicationRoot()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Get user home directory
        /// </summary>
        public static string GetHomeDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        /// <summary>
        /// Get temp directory
        /// </summary>
        public static string GetTempDirectory()
        {
            return Path.GetTempPath();
        }

        /// <summary>
        /// Get current working directory
        /// </summary>
        public static string GetCurrentDirectory()
        {
            return Environment.CurrentDirectory;
        }

        /// <summary>
        /// Check if running on Windows
        /// </summary>
        public static bool IsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }

        /// <summary>
        /// Check if running on Unix/Linux/Mac
        /// </summary>
        public static bool IsUnix()
        {
            return Environment.OSVersion.Platform == PlatformID.Unix ||
                   Environment.OSVersion.Platform == PlatformID.MacOSX;
        }

        /// <summary>
        /// Get number of available processors
        /// </summary>
        public static int GetProcessorCount()
        {
            return Environment.ProcessorCount;
        }

        /// <summary>
        /// Get application version
        /// </summary>
        public static string GetApplicationVersion()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var version = assembly?.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
    }
}
