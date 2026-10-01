#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;
using CaddyVpsToolkit.Integration;
using CaddyVpsToolkit.Results;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Manages domain-to-service mappings in the Caddy reverse proxy.
    /// Handles adding, removing, and listing domains with their upstream targets.
    /// </summary>
    public sealed class DomainManager
    {
        private readonly ICaddyClient _caddyClient;
        private readonly CaddyConfigurationService _configService;
        private readonly Dictionary<string, DomainMapping> _mappings = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainManager"/> class.
        /// </summary>
        /// <param name="caddyClient">Client for the Caddy admin API.</param>
        /// <param name="configService">Service for Caddyfile generation.</param>
        public DomainManager(ICaddyClient caddyClient, CaddyConfigurationService configService)
        {
            _caddyClient = caddyClient ?? throw new ArgumentNullException(nameof(caddyClient));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        /// <summary>
        /// Registers a new domain pointing to an upstream service.
        /// </summary>
        /// <param name="domain">Fully qualified domain name (e.g. <c>app.example.com</c>).</param>
        /// <param name="upstreamHost">Upstream host including port (e.g. <c>localhost:8080</c>).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A result indicating success or the reason for failure.</returns>
        public async Task<Result> AddDomainAsync(string domain, string upstreamHost, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return Result.Failure("Domain must not be empty.");

            if (string.IsNullOrWhiteSpace(upstreamHost))
                return Result.Failure("Upstream host must not be empty.");

            var normalized = NormalizeDomain(domain);

            if (_mappings.ContainsKey(normalized))
                return Result.Failure($"Domain '{normalized}' is already registered.");

            var route = new CaddyRoute
            {
                Id = $"route-{normalized.Replace(".", "-")}",
                Domain = normalized,
                UpstreamUrl = upstreamHost,
                EnableHttps = true
            };

            var applied = await _caddyClient.UpsertRouteAsync(route, ct);
            if (!applied)
                return Result.Failure($"Caddy admin API rejected the route for '{normalized}'.");

            _mappings[normalized] = new DomainMapping(normalized, upstreamHost, DateTime.UtcNow);
            return Result.Success();
        }

        /// <summary>
        /// Removes a previously registered domain and its route.
        /// </summary>
        /// <param name="domain">The domain to remove.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A result indicating success or the reason for failure.</returns>
        public async Task<Result> RemoveDomainAsync(string domain, CancellationToken ct = default)
        {
            var normalized = NormalizeDomain(domain);

            if (!_mappings.ContainsKey(normalized))
                return Result.Failure($"Domain '{normalized}' is not registered.");

            var routeId = $"route-{normalized.Replace(".", "-")}";
            var removed = await _caddyClient.RemoveRouteAsync(routeId, ct);
            if (!removed)
                return Result.Failure($"Caddy admin API failed to remove route for '{normalized}'.");

            _mappings.Remove(normalized);
            return Result.Success();
        }

        /// <summary>
        /// Returns all currently registered domain mappings.
        /// </summary>
        public IReadOnlyList<DomainMapping> ListDomains()
        {
            return _mappings.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Checks whether a domain is already registered.
        /// </summary>
        /// <param name="domain">The domain to check.</param>
        /// <returns><c>true</c> if the domain has a mapping.</returns>
        public bool IsDomainRegistered(string domain)
        {
            return _mappings.ContainsKey(NormalizeDomain(domain));
        }

        private static string NormalizeDomain(string domain)
        {
            return domain.Trim().ToLowerInvariant().TrimEnd('.');
        }
    }

    /// <summary>
    /// Represents a domain-to-upstream mapping with metadata.
    /// </summary>
    /// <param name="Domain">Fully qualified domain name.</param>
    /// <param name="Upstream">Upstream host:port target.</param>
    /// <param name="CreatedAtUtc">When the mapping was created.</param>
    public readonly record struct DomainMapping(string Domain, string Upstream, DateTime CreatedAtUtc);
}
