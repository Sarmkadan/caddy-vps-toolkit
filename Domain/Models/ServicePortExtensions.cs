#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CaddyVpsToolkit.Domain.Models
{
    /// <summary>
    /// Extension methods for <see cref="ServicePort"/> providing common port operations
    /// </summary>
    public static class ServicePortExtensions
    {
        /// <summary>
        /// Determines whether this port mapping is a privileged port (ports 1-1023)
        /// </summary>
        /// <param name="port">The service port instance</param>
        /// <returns>True if the port is privileged; otherwise, false</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="port"/> is null</exception>
        public static bool IsPrivilegedPort(this ServicePort port)
        {
            ArgumentNullException.ThrowIfNull(port);
            return port.ExternalPort is >= 1 and <= 1023;
        }

        /// <summary>
        /// Determines whether this port mapping is a well-known port (ports 0-1023)
        /// </summary>
        /// <param name="port">The service port instance</param>
        /// <returns>True if the port is a well-known port; otherwise, false</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="port"/> is null</exception>
        public static bool IsWellKnownPort(this ServicePort port)
        {
            ArgumentNullException.ThrowIfNull(port);
            return port.IsPrivilegedPort();
        }

        /// <summary>
        /// Determines whether this port mapping is a registered port (ports 1024-49151)
        /// </summary>
        /// <param name="port">The service port instance</param>
        /// <returns>True if the port is a registered port; otherwise, false</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="port"/> is null</exception>
        public static bool IsRegisteredPort(this ServicePort port)
        {
            ArgumentNullException.ThrowIfNull(port);
            return port.ExternalPort is >= 1024 and <= 49151;
        }

        /// <summary>
        /// Determines whether this port mapping is a dynamic/private port (ports 49152-65535)
        /// </summary>
        /// <param name="port">The service port instance</param>
        /// <returns>True if the port is a dynamic/private port; otherwise, false</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="port"/> is null</exception>
        public static bool IsDynamicPort(this ServicePort port)
        {
            ArgumentNullException.ThrowIfNull(port);
            return port.ExternalPort is >= 49152 and <= 65535;
        }

        /// <summary>
        /// Gets the IANA service name for this port if it's a well-known port
        /// </summary>
        /// <param name="port">The service port instance</param>
        /// <returns>IANA service name if available; otherwise, null</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="port"/> is null</exception>
        public static string? GetIanaServiceName(this ServicePort port)
        {
            ArgumentNullException.ThrowIfNull(port);

            return port.ExternalPort switch
            {
                20 => "ftp-data",
                21 => "ftp",
                22 => "ssh",
                23 => "telnet",
                25 => "smtp",
                53 => "domain",
                80 => "http",
                110 => "pop3",
                123 => "ntp",
                143 => "imap",
                161 => "snmp",
                194 => "irc",
                389 => "ldap",
                443 => "https",
                445 => "microsoft-ds",
                465 => "smtps",
                514 => "syslog",
                587 => "submission",
                636 => "ldaps",
                993 => "imaps",
                995 => "pop3s",
                1433 => "ms-sql-s",
                1521 => "oracle-db",
                3306 => "mysql",
                3389 => "ms-wbt-server",
                5432 => "postgresql",
                5900 => "vnc",
                6379 => "redis",
                8080 => "http-alt",
                _ => null
            };
        }

        /// <summary>
        /// Gets the port category based on IANA classification
        /// </summary>
        /// <param name="port">The service port instance</param>
        /// <returns>Port category description</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="port"/> is null</exception>
        public static string GetPortCategory(this ServicePort port)
        {
            ArgumentNullException.ThrowIfNull(port);

            return port.ExternalPort switch
            {
                <= 1023 => "Well-known port",
                >= 1024 and <= 49151 => "Registered port",
                >= 49152 and <= 65535 => "Dynamic/Private port",
                _ => "Reserved"
            };
        }

        /// <summary>
        /// Determines whether this port mapping is commonly used for HTTP traffic
        /// </summary>
        /// <param name="port">The service port instance</param>
        /// <returns>True if the port is commonly used for HTTP traffic; otherwise, false</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="port"/> is null</exception>
        public static bool IsHttpPort(this ServicePort port)
        {
            ArgumentNullException.ThrowIfNull(port);
            return port.ExternalPort is 80 or 443 or 8080 or 8443;
        }

        /// <summary>
        /// Gets all port mappings that share the same internal port
        /// </summary>
        /// <param name="ports">The collection of service ports to search</param>
        /// <param name="internalPort">The internal port to match</param>
        /// <returns>Read-only list of matching port mappings</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ports"/> is null</exception>
        public static IReadOnlyList<ServicePort> GetPortsByInternalPort(this IEnumerable<ServicePort> ports, int internalPort)
        {
            ArgumentNullException.ThrowIfNull(ports);

            var result = new List<ServicePort>();
            foreach (var port in ports)
            {
                if (port.InternalPort == internalPort)
                {
                    result.Add(port);
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>
        /// Gets all port mappings that share the same external port
        /// </summary>
        /// <param name="ports">The collection of service ports to search</param>
        /// <param name="externalPort">The external port to match</param>
        /// <returns>Read-only list of matching port mappings</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ports"/> is null</exception>
        public static IReadOnlyList<ServicePort> GetPortsByExternalPort(this IEnumerable<ServicePort> ports, int externalPort)
        {
            ArgumentNullException.ThrowIfNull(ports);

            var result = new List<ServicePort>();
            foreach (var port in ports)
            {
                if (port.ExternalPort == externalPort)
                {
                    result.Add(port);
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>
        /// Determines whether this port mapping conflicts with another port mapping
        /// (same external port and protocol)
        /// </summary>
        /// <param name="port">The service port instance</param>
        /// <param name="other">The other port to check against</param>
        /// <returns>True if there's a conflict; otherwise, false</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="port"/> or <paramref name="other"/> is null</exception>
        public static bool ConflictsWith(this ServicePort port, ServicePort other)
        {
            ArgumentNullException.ThrowIfNull(port);
            ArgumentNullException.ThrowIfNull(other);

            return port.ExternalPort == other.ExternalPort && port.Protocol == other.Protocol;
        }

        /// <summary>
        /// Gets a formatted port description including protocol and category
        /// </summary>
        /// <param name="port">The service port instance</param>
        /// <returns>Formatted port description</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="port"/> is null</exception>
        public static string GetFormattedDescription(this ServicePort port)
        {
            ArgumentNullException.ThrowIfNull(port);

            var ianaName = port.GetIanaServiceName();
            var category = port.GetPortCategory();

            return string.IsNullOrEmpty(ianaName)
                ? $"{port.InternalPort} → {port.ExternalPort}/{port.Protocol} ({category})"
                : $"{port.InternalPort} → {port.ExternalPort}/{port.Protocol} ({category}, {ianaName})" +
                  (string.IsNullOrEmpty(port.Description) ? string.Empty : $" - " + port.Description);
        }
    }
}