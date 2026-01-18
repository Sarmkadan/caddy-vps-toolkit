// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace CaddyVpsToolkit.Core
{
    public static class AppConstants
    {
        public const string AppName = "Caddy VPS Toolkit";
        public const string AppVersion = "1.0.0";
        public const string ApplicationId = "caddy-vps-toolkit";

        // Directories
        public static readonly string ConfigDirectory = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%/caddy-vps-toolkit");
        public static readonly string LogsDirectory = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%/caddy-vps-toolkit/logs");
        public static readonly string CacheDirectory = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%/caddy-vps-toolkit/cache");
        public static readonly string SystemdUnitsDirectory = "/etc/systemd/system";
        public static readonly string CaddyConfigDirectory = "/etc/caddy";
        public static readonly string CaddyfilePath = "/etc/caddy/Caddyfile";
        public static readonly string DatabasePath = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%/caddy-vps-toolkit/services.db");

        // Ports
        public const int DefaultCaddyAdminPort = 2019;
        public const int DefaultHttpPort = 80;
        public const int DefaultHttpsPort = 443;
        public const int DefaultServicePort = 8080;

        // Defaults
        public const int DefaultHealthCheckIntervalSeconds = 30;
        public const int DefaultHealthCheckTimeoutSeconds = 5;
        public const int DefaultHealthCheckUnhealthyThreshold = 3;
        public const string DefaultServiceUser = "root";

        // File names
        public const string CaddyfileName = "Caddyfile";
        public const string AppSettingsFileName = "appsettings.json";
        public const string SystemdUnitFileSuffix = ".service";

        // CLI
        public const int DefaultCommandTimeoutSeconds = 30;

        // Validation
        public const int MaxServiceNameLength = 255;
        public const int MinServiceNameLength = 3;
        public const int MaxPortNumber = 65535;
        public const int MinPortNumber = 1;

        // Timeouts
        public const int HealthCheckSocketTimeoutMs = 5000;
        public const int SystemdCommandTimeoutSeconds = 10;
        public const int CaddyAdminApiTimeoutSeconds = 5;
    }

    public static class ErrorMessages
    {
        public const string ServiceNotFound = "Service not found";
        public const string ServiceAlreadyExists = "Service already exists";
        public const string InvalidServiceConfiguration = "Invalid service configuration";
        public const string FailedToStartService = "Failed to start service";
        public const string FailedToStopService = "Failed to stop service";
        public const string FailedToRestartService = "Failed to restart service";
        public const string SystemdNotAvailable = "Systemd is not available";
        public const string CaddyNotInstalled = "Caddy is not installed";
        public const string HealthCheckFailed = "Health check failed";
        public const string DatabaseError = "Database error occurred";
    }
}
