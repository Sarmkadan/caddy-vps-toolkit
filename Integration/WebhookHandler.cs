// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CaddyVpsToolkit.Integration
{
    /// <summary>
    /// Webhook event types that can be triggered by the system
    /// </summary>
    public enum WebhookEventType
    {
        ServiceCreated,
        ServiceDeleted,
        ServiceStatusChanged,
        HealthCheckFailed,
        HealthCheckRecovered,
        ConfigurationUpdated
    }

    /// <summary>
    /// Webhook registration and delivery system.
    /// Allows external systems to be notified of important events.
    /// </summary>
    public interface IWebhookHandler
    {
        void Register(string url, WebhookEventType eventType);
        void Unregister(string url, WebhookEventType eventType);
        Task<bool> TriggerAsync(WebhookEventType eventType, object payload);
    }

    public class WebhookHandler : IWebhookHandler
    {
        private readonly Dictionary<WebhookEventType, List<string>> _registrations = new();
        private readonly IHttpClient _httpClient;

        public WebhookHandler(IHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public void Register(string url, WebhookEventType eventType)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL required", nameof(url));

            if (!_registrations.ContainsKey(eventType))
                _registrations[eventType] = new List<string>();

            if (!_registrations[eventType].Contains(url))
                _registrations[eventType].Add(url);
        }

        public void Unregister(string url, WebhookEventType eventType)
        {
            if (_registrations.ContainsKey(eventType))
                _registrations[eventType].Remove(url);
        }

        public async Task<bool> TriggerAsync(WebhookEventType eventType, object payload)
        {
            if (!_registrations.TryGetValue(eventType, out var urls))
                return true;

            var webhookData = new
            {
                EventType = eventType.ToString(),
                Timestamp = DateTime.UtcNow,
                Payload = payload
            };

            bool allSucceeded = true;

            foreach (var url in urls)
            {
                try
                {
                    var response = await _httpClient.PostAsync<object>(
                        url,
                        webhookData
                    );

                    if (!response.IsSuccess)
                        allSucceeded = false;
                }
                catch
                {
                    allSucceeded = false;
                }
            }

            return allSucceeded;
        }

        public List<string> GetRegistrations(WebhookEventType eventType)
        {
            return _registrations.TryGetValue(eventType, out var urls)
                ? new List<string>(urls)
                : new List<string>();
        }
    }

    /// <summary>
    /// Standard webhook payload format
    /// </summary>
    public class WebhookPayload
    {
        [JsonProperty("event_type")]
        public string EventType { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("data")]
        public Dictionary<string, object> Data { get; set; }

        public WebhookPayload()
        {
            Data = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }
    }
}
