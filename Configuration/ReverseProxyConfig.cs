#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Core.Exceptions;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Configuration
{
    /// <summary>
    /// Strongly-typed representation of a reverse proxy configuration block
    /// that can be serialized to/from JSON and rendered as a Caddyfile snippet.
    /// </summary>
    public sealed class ReverseProxyConfig
    {
        private static readonly string[] SupportedLoadBalancingPolicies =
        {
            "random", "round_robin", "least_conn", "first", "ip_hash", "uri_hash", "header", "cookie"
        };

        // host or [IPv6], followed by :port (1-5 digits; range checked separately)
        private static readonly Regex HostPortPattern = new(
            @"^(?<host>[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?)*|\[[0-9A-Fa-f:.]+\]):(?<port>[0-9]{1,5})\z",
            RegexOptions.CultureInvariant);

        // http(s)://authority with optional trailing slash and no path, query, or fragment
        private static readonly Regex UpstreamUrlPattern = new(
            @"^https?://(?<authority>[^/?#]+)/?\z",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private readonly List<UpstreamEntry> _upstreams = new();

        /// <summary>Domain this reverse proxy block serves.</summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>Whether to enable automatic HTTPS via ACME.</summary>
        public bool AutoHttps { get; set; } = true;

        /// <summary>
        /// Optional path matcher. When set, only requests matching this prefix are proxied.
        /// </summary>
        public string? PathPrefix { get; set; }

        /// <summary>Read-only view of the upstream targets.</summary>
        public IReadOnlyList<UpstreamEntry> Upstreams => _upstreams.AsReadOnly();

        /// <summary>Additional headers to inject into proxied requests.</summary>
        public Dictionary<string, string> ExtraHeaders { get; set; } = new();

        /// <summary>
        /// Caddy <c>lb_policy</c> to render, e.g. <c>round_robin</c>. Null leaves the policy unset,
        /// which renders the weighted round-robin fallback when upstream weights differ.
        /// </summary>
        public string? LoadBalancingPolicy { get; set; }

        /// <summary>Seconds allowed to establish a connection to an upstream. Must be positive.</summary>
        public int DialTimeoutSeconds { get; set; } = 10;

        /// <summary>Seconds allowed for an upstream to send response headers. Must be positive.</summary>
        public int ResponseHeaderTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Adds an upstream target to this reverse proxy block.
        /// </summary>
        /// <param name="hostPort">Host and port, e.g. <c>localhost:8080</c>.</param>
        /// <param name="weight">Relative weight for weighted round-robin (default 1).</param>
        public void AddUpstream(string hostPort, int weight = 1)
        {
            if (string.IsNullOrWhiteSpace(hostPort))
                throw new ArgumentException("Upstream host:port must not be empty.", nameof(hostPort));

            if (weight < 1)
                throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be at least 1.");

            _upstreams.Add(new UpstreamEntry(hostPort, weight));
        }

        /// <summary>
        /// Removes an upstream target by its host:port.
        /// </summary>
        /// <param name="hostPort">The upstream to remove.</param>
        /// <returns><c>true</c> if found and removed.</returns>
        public bool RemoveUpstream(string hostPort)
        {
            var idx = _upstreams.FindIndex(u =>
                u.HostPort.Equals(hostPort, StringComparison.OrdinalIgnoreCase));

            if (idx < 0) return false;
            _upstreams.RemoveAt(idx);
            return true;
        }

        /// <summary>
        /// Checks every rule for this configuration and collects all violations.
        /// </summary>
        /// <returns>The list of problems; empty when the configuration is valid.</returns>
        public IReadOnlyList<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Domain))
                errors.Add("Domain must be set.");

            if (_upstreams.Count == 0)
                errors.Add("At least one upstream is required.");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var reportedDuplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var upstream in _upstreams)
            {
                var hostPort = (upstream.HostPort ?? string.Empty).Trim();

                if (!IsValidUpstream(hostPort))
                    errors.Add($"Upstream '{hostPort}' must be host:port or http(s)://host:port with a port from {AppConstants.MinPortNumber} to {AppConstants.MaxPortNumber}.");

                if (!seen.Add(hostPort) && reportedDuplicates.Add(hostPort))
                    errors.Add($"Upstream '{hostPort}' is defined more than once.");
            }

            if (LoadBalancingPolicy is not null &&
                Array.IndexOf(SupportedLoadBalancingPolicies, LoadBalancingPolicy) < 0)
            {
                errors.Add($"Load-balancing policy '{LoadBalancingPolicy}' is not supported. Use one of: {string.Join(", ", SupportedLoadBalancingPolicies)}.");
            }

            if (DialTimeoutSeconds <= 0)
                errors.Add($"Dial timeout must be a positive number of seconds, but was {DialTimeoutSeconds}.");

            if (ResponseHeaderTimeoutSeconds <= 0)
                errors.Add($"Response header timeout must be a positive number of seconds, but was {ResponseHeaderTimeoutSeconds}.");

            return errors;
        }

        /// <summary>
        /// Validates this configuration and throws if any rule is violated.
        /// All problems are reported together in a single exception.
        /// </summary>
        /// <exception cref="ReverseProxyConfigValidationException">Thrown when one or more rules fail.</exception>
        public void Validate()
        {
            var errors = GetValidationErrors();
            if (errors.Count > 0)
                throw new ReverseProxyConfigValidationException(errors);
        }

        /// <summary>
        /// Renders this configuration as a Caddyfile-compatible text block.
        /// </summary>
        /// <returns>A string ready to be appended to a Caddyfile.</returns>
        /// <exception cref="ReverseProxyConfigValidationException">Thrown when the configuration is invalid.</exception>
        public string ToCaddyfileSnippet()
        {
            Validate();

            var sb = new StringBuilder();
            sb.AppendLine($"{Domain} {{");

            var matcher = string.IsNullOrWhiteSpace(PathPrefix) ? "" : $" {PathPrefix}*";
            var targets = string.Join(" ", _upstreams.Select(u => u.HostPort));
            sb.AppendLine($"    reverse_proxy{matcher} {targets} {{");

            if (LoadBalancingPolicy is not null)
            {
                sb.AppendLine($"        lb_policy {LoadBalancingPolicy}");
            }
            else if (_upstreams.Any(u => u.Weight != 1))
            {
                sb.AppendLine("        lb_policy weighted_round_robin");
                foreach (var u in _upstreams)
                    sb.AppendLine($"        # weight {u.HostPort} = {u.Weight}");
            }

            sb.AppendLine("        transport http {");
            sb.AppendLine($"            dial_timeout {DialTimeoutSeconds}s");
            sb.AppendLine($"            response_header_timeout {ResponseHeaderTimeoutSeconds}s");
            sb.AppendLine("        }");
            sb.AppendLine("        health_uri /health");
            sb.AppendLine($"        health_interval {AppConstants.DefaultHealthCheckIntervalSeconds}s");
            sb.AppendLine("    }");

            foreach (var (key, value) in ExtraHeaders)
                sb.AppendLine($"    header {key} \"{value}\"");

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static bool IsValidUpstream(string value)
        {
            var urlMatch = UpstreamUrlPattern.Match(value);
            var candidate = urlMatch.Success ? urlMatch.Groups["authority"].Value : value;

            var hostPortMatch = HostPortPattern.Match(candidate);
            if (!hostPortMatch.Success)
                return false;

            return int.TryParse(hostPortMatch.Groups["port"].Value, out var port)
                && port >= AppConstants.MinPortNumber
                && port <= AppConstants.MaxPortNumber;
        }

        /// <summary>
        /// Serializes this configuration to a JSON string.
        /// </summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Deserializes a <see cref="ReverseProxyConfig"/> from a JSON string.
        /// </summary>
        /// <param name="json">The JSON payload.</param>
        /// <returns>A populated <see cref="ReverseProxyConfig"/> instance.</returns>
        public static ReverseProxyConfig FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON must not be empty.", nameof(json));

            return JsonSerializer.Deserialize<ReverseProxyConfig>(json)
                ?? throw new InvalidOperationException("Deserialization returned null.");
        }
    }

    /// <summary>
    /// A single upstream target with an optional routing weight.
    /// </summary>
    /// <param name="HostPort">Upstream address, e.g. <c>127.0.0.1:3000</c>.</param>
    /// <param name="Weight">Relative weight for load balancing.</param>
    public readonly record struct UpstreamEntry(string HostPort, int Weight = 1);
}
