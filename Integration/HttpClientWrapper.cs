#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CaddyVpsToolkit.Utilities;

namespace CaddyVpsToolkit.Integration
{
    /// <summary>
    /// Wrapper around HttpClient with built‑in retry, timeout, and error handling.
    /// Abstracts HTTP communication for cleaner service code.
    /// </summary>
    public interface IHttpClient
    {
        Task<HttpResponse<T>> GetAsync<T>(string url, Dictionary<string, string>? headers = null);
        Task<HttpResponse<T>> PostAsync<T>(string url, object data, Dictionary<string, string>? headers = null);
        Task<HttpResponse<T>> PutAsync<T>(string url, object data, Dictionary<string, string>? headers = null);
        Task<HttpResponse<string>> DeleteAsync(string url, Dictionary<string, string>? headers = null);
    }

    public sealed class HttpClientWrapper : IHttpClient
    {
        private readonly HttpClient _client;
        private readonly IRetryPolicy _retryPolicy;
        private readonly int _timeoutMs;

        /// <summary>
        /// Primary constructor that receives a fully configured <see cref="HttpClientWrapperOptions"/>
        /// instance. This moves the previously hard‑coded timeout and retry policy into a
        /// strongly‑typed options class that can be bound from configuration.
        /// </summary>
        /// <param name="options">Configuration options; if <c>null</c>, defaults are used.</param>
        public HttpClientWrapper(HttpClientWrapperOptions? options = null)
        {
            var opts = options ?? new HttpClientWrapperOptions();

            _timeoutMs = opts.TimeoutMs;
            _retryPolicy = opts.RetryPolicy ?? new NoRetryPolicy();

            _client = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(_timeoutMs)
            };
        }

        /// <summary>
        /// Backward‑compatible overload that mirrors the original signature.
        /// It forwards to the options‑based constructor, preserving existing call‑sites.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds (default 30 000).</param>
        /// <param name="retryPolicy">Retry policy; if <c>null</c>, a <see cref="NoRetryPolicy"/> is used.</param>
        public HttpClientWrapper(int timeoutMs = 30_000, IRetryPolicy? retryPolicy = null)
            : this(new HttpClientWrapperOptions { TimeoutMs = timeoutMs, RetryPolicy = retryPolicy })
        {
        }

        public async Task<HttpResponse<T>> GetAsync<T>(
            string url,
            Dictionary<string, string>? headers = null)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));
            if (headers != null)
            {
                foreach (var kvp in headers)
                {
                    if (kvp.Value == null)
                        throw new ArgumentException("Header value cannot be null", nameof(kvp.Value));
                }
            }
            return await ExecuteAsync<T>(async () =>
            {
                var request = CreateRequest(HttpMethod.Get, url, headers);
                var response = await _client.SendAsync(request);
                return await ParseResponse<T>(response);
            });
        }

        public async Task<HttpResponse<T>> PostAsync<T>(
            string url,
            object data,
            Dictionary<string, string>? headers = null)
        {
            return await ExecuteAsync<T>(async () =>
            {
                var request = CreateRequest(HttpMethod.Post, url, headers);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(data),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );
                var response = await _client.SendAsync(request);
                return await ParseResponse<T>(response);
            });
        }

        public async Task<HttpResponse<T>> PutAsync<T>(
            string url,
            object data,
            Dictionary<string, string>? headers = null)
        {
            return await ExecuteAsync<T>(async () =>
            {
                var request = CreateRequest(HttpMethod.Put, url, headers);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(data),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );
                var response = await _client.SendAsync(request);
                return await ParseResponse<T>(response);
            });
        }

        public async Task<HttpResponse<string>> DeleteAsync(
            string url,
            Dictionary<string, string>? headers = null)
        {
            return await ExecuteAsync<string>(async () =>
            {
                var request = CreateRequest(HttpMethod.Delete, url, headers);
                var response = await _client.SendAsync(request);
                return await ParseResponse<string>(response);
            });
        }

        private HttpRequestMessage CreateRequest(
            HttpMethod method,
            string url,
            Dictionary<string, string>? headers)
        {
            var request = new HttpRequestMessage(method, url);

            if (headers is not null)
            {
                foreach (var kvp in headers)
                    request.Headers.Add(kvp.Key, kvp.Value);
            }

            return request;
        }

        private async Task<HttpResponse<T>> ParseResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            try
            {
                var data = typeof(T) == typeof(string)
                    ? (T)(object)content
                    : JsonSerializer.Deserialize<T>(content);

                return new HttpResponse<T>
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode,
                    Data = data,
                    RawContent = content
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse<T>
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = false,
                    Error = ex.Message,
                    RawContent = content
                };
            }
        }

        private async Task<HttpResponse<T>> ExecuteAsync<T>(Func<Task<HttpResponse<T>>> operation)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(operation);
            }
            catch (Exception ex)
            {
                return new HttpResponse<T>
                {
                    IsSuccess = false,
                    Error = ex.Message
                };
            }
        }
    }

    /// <summary>
    /// HTTP response wrapper with status, data, and error information
    /// </summary>
    public sealed class HttpResponse<T>
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
        public string? RawContent { get; set; }
    }
}
