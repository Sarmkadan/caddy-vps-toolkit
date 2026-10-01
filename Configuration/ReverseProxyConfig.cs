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
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Configuration
{
    /// <summary>
    /// Strongly-typed representation of a reverse proxy configuration block
    /// that can be serialized to/from JSON and rendered as a Caddyfile snippet.
    /// </summary>
    public sealed class ReverseProxyConfig
    {
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
        /// Renders this configuration as a Caddyfile-compatible text block.
        /// </summary>
        /// <returns>A string ready to be appended to a Caddyfile.</returns>
        public string ToCaddyfileSnippet()
        {
            if (string.IsNullOrWhiteSpace(Domain))
                throw new InvalidOperationException("Domain must be set before rendering.");

            if (_upstreams.Count == 0)
                throw new InvalidOperationException("At least one upstream is required.");

            var sb = new StringBuilder();
            sb.AppendLine($"{Domain} {{");

            var matcher = string.IsNullOrWhiteSpace(PathPrefix) ? "" : $" {PathPrefix}*";
            var targets = string.Join(" ", _upstreams.Select(u => u.HostPort));
            sb.AppendLine($"    reverse_proxy{matcher} {targets} {{");

            if (_upstreams.Any(u => u.Weight != 1))
            {
                sb.AppendLine("        lb_policy weighted_round_robin");
                foreach (var u in _upstreams)
                    sb.AppendLine($"        # weight {u.HostPort} = {u.Weight}");
            }

            sb.AppendLine("        health_uri /health");
            sb.AppendLine($"        health_interval {AppConstants.DefaultHealthCheckIntervalSeconds}s");
            sb.AppendLine("    }");

            foreach (var (key, value) in ExtraHeaders)
                sb.AppendLine($"    header {key} \"{value}\"");

            sb.AppendLine("}");
            return sb.ToString();
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
