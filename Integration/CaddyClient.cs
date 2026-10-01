#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Integration
{
    /// <summary>
    /// Client for communicating with the Caddy admin API (localhost:2019 by default).
    /// Provides typed methods for configuration reload, route management, and health queries.
    /// </summary>
    public interface ICaddyClient
    {
        /// <summary>Loads the full Caddy configuration from the admin API.</summary>
        Task<CaddyConfig?> GetConfigAsync(CancellationToken ct = default);

        /// <summary>Pushes a complete Caddy configuration, replacing the running one.</summary>
        Task<bool> ApplyConfigAsync(CaddyConfig config, CancellationToken ct = default);

        /// <summary>Adds or updates a single route identified by its ID.</summary>
        Task<bool> UpsertRouteAsync(CaddyRoute route, CancellationToken ct = default);

        /// <summary>Removes a route by its identifier.</summary>
        Task<bool> RemoveRouteAsync(string routeId, CancellationToken ct = default);

        /// <summary>Returns <c>true</c> when the Caddy admin endpoint responds successfully.</summary>
        Task<bool> IsHealthyAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Default implementation of <see cref="ICaddyClient"/> that talks to the Caddy
    /// admin REST API over HTTP via <see cref="IHttpClient"/>.
    /// </summary>
    public sealed class CaddyClient : ICaddyClient, IDisposable
    {
        private readonly IHttpClient _http;
        private readonly string _baseUrl;
        private bool _disposed;

        /// <summary>
        /// Creates a new <see cref="CaddyClient"/> pointing at the given admin endpoint.
        /// </summary>
        /// <param name="http">HTTP client used for all requests.</param>
        /// <param name="adminHost">Caddy admin host (default <c>localhost</c>).</param>
        /// <param name="adminPort">Caddy admin port (default <c>2019</c>).</param>
        public CaddyClient(
            IHttpClient http,
            string adminHost = "localhost",
            int adminPort = AppConstants.DefaultCaddyAdminPort)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));

            if (adminPort < AppConstants.MinPortNumber || adminPort > AppConstants.MaxPortNumber)
                throw new ArgumentOutOfRangeException(nameof(adminPort));

            _baseUrl = $"http://{adminHost}:{adminPort}";
        }

        /// <inheritdoc />
        public async Task<CaddyConfig?> GetConfigAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            var response = await _http.GetAsync<CaddyConfig>($"{_baseUrl}/config/");
            return response.IsSuccess ? response.Data : null;
        }

        /// <inheritdoc />
        public async Task<bool> ApplyConfigAsync(CaddyConfig config, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (config is null) throw new ArgumentNullException(nameof(config));

            var response = await _http.PostAsync<string>($"{_baseUrl}/load", config);
            return response.IsSuccess;
        }

        /// <inheritdoc />
        public async Task<bool> UpsertRouteAsync(CaddyRoute route, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (route is null) throw new ArgumentNullException(nameof(route));

            var path = string.IsNullOrWhiteSpace(route.Id)
                ? $"{_baseUrl}/config/apps/http/servers/srv0/routes"
                : $"{_baseUrl}/id/{route.Id}";

            var response = string.IsNullOrWhiteSpace(route.Id)
                ? await _http.PostAsync<string>(path, route)
                : await _http.PutAsync<string>(path, route);

            return response.IsSuccess;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveRouteAsync(string routeId, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(routeId))
                throw new ArgumentException("Route ID must not be empty.", nameof(routeId));

            var response = await _http.DeleteAsync($"{_baseUrl}/id/{routeId}");
            return response.IsSuccess;
        }

        /// <inheritdoc />
        public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            try
            {
                var response = await _http.GetAsync<string>($"{_baseUrl}/config/");
                return response.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Releases the underlying HTTP resources.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CaddyClient));
        }
    }
}
