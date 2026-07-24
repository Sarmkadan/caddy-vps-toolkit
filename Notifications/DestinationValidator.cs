#nullable enable

using System;
using System.Net;
using System.Text.RegularExpressions;

namespace CaddyVpsToolkit.Notifications
{
    /// <summary>
    /// Validates destination strings for notification providers (email, webhook URL, phone numbers).
    /// Provides format validation and security checks to prevent SSRF and injection attacks.
    /// </summary>
    public static class DestinationValidator
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
            RegexOptions.Compiled);

        private static readonly Regex PhoneRegex = new Regex(
            @"^\+?[0-9\s\-\(\)]{8,}$",
            RegexOptions.Compiled);

        private static readonly Regex TemplateInjectionPattern = new Regex(
            @"\{\{[^}]+\}|\%[a-zA-Z][a-zA-Z0-9]*\$",
            RegexOptions.Compiled);

        /// <summary>
        /// Validates an email address destination.
        /// </summary>
        /// <param name="email">The email address to validate</param>
        /// <param name="allowNullOrEmpty">Whether to allow null or empty strings</param>
        /// <returns>The validated email address</returns>
        /// <exception cref="ArgumentException">Thrown when email format is invalid</exception>
        public static string ValidateEmail(string email, bool allowNullOrEmpty = false)
        {
            if (allowNullOrEmpty && string.IsNullOrEmpty(email))
                return email;

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email address cannot be null or empty", nameof(email));

            if (!EmailRegex.IsMatch(email))
                throw new ArgumentException($"Invalid email format: '{email}'", nameof(email));

            return email.Trim();
        }

        /// <summary>
        /// Validates a webhook URL destination.
        /// </summary>
        /// <param name="webhookUrl">The webhook URL to validate</param>
        /// <param name="allowNullOrEmpty">Whether to allow null or empty strings</param>
        /// <returns>The validated webhook URL</returns>
        /// <exception cref="ArgumentException">Thrown when URL format is invalid or contains blocked patterns</exception>
        public static string ValidateWebhookUrl(string webhookUrl, bool allowNullOrEmpty = false)
        {
            if (allowNullOrEmpty && string.IsNullOrEmpty(webhookUrl))
                return webhookUrl;

            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new ArgumentException("Webhook URL cannot be null or empty", nameof(webhookUrl));

            webhookUrl = webhookUrl.Trim();

            // Validate basic URL format
            if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException($"Invalid webhook URL format: '{webhookUrl}'", nameof(webhookUrl));

            // Validate scheme (only http and https are allowed)
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException($"Webhook URL must use http or https scheme, got: '{uri.Scheme}'", nameof(webhookUrl));

            // Check for localhost and private IP ranges (SSRF protection)
            if (IsLocalOrPrivateAddress(uri.Host))
                throw new ArgumentException($"Webhook URL points to localhost or private address '{uri.Host}', which is not allowed for security reasons", nameof(webhookUrl));

            // Validate hostname is not empty
            if (string.IsNullOrEmpty(uri.Host))
                throw new ArgumentException("Webhook URL must contain a valid hostname", nameof(webhookUrl));

            return webhookUrl;
        }

        /// <summary>
        /// Validates a phone number destination.
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate</param>
        /// <param name="allowNullOrEmpty">Whether to allow null or empty strings</param>
        /// <returns>The validated phone number</returns>
        /// <exception cref="ArgumentException">Thrown when phone number format is invalid</exception>
        public static string ValidatePhoneNumber(string phoneNumber, bool allowNullOrEmpty = false)
        {
            if (allowNullOrEmpty && string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be null or empty", nameof(phoneNumber));

            var normalized = phoneNumber.Trim();

            if (!PhoneRegex.IsMatch(normalized))
                throw new ArgumentException($"Invalid phone number format: '{phoneNumber}'. Expected format: +1234567890 or 123-456-7890", nameof(phoneNumber));

            return normalized;
        }

        /// <summary>
        /// Validates notification message content to prevent template injection.
        /// Ensures message content is treated as literal text and not evaluated.
        /// </summary>
        /// <param name="message">The message content to validate</param>
        /// <param name="allowNullOrEmpty">Whether to allow null or empty strings</param>
        /// <returns>The validated message content</returns>
        /// <exception cref="ArgumentException">Thrown when message contains template injection patterns</exception>
        public static string ValidateMessageContent(string message, bool allowNullOrEmpty = false)
        {
            if (allowNullOrEmpty && string.IsNullOrEmpty(message))
                return message;

            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Message content cannot be null or empty", nameof(message));

            var content = message.Trim();

            // Check for template injection patterns like {{...}} or format string tokens
            if (TemplateInjectionPattern.IsMatch(content))
                throw new ArgumentException(
                    "Message content contains template injection patterns ({{...}} or format tokens). " +
                    "Message content will be sent literally and not evaluated as a template.",
                    nameof(message));

            return content;
        }

        /// <summary>
        /// Validates a destination string based on its type.
        /// </summary>
        /// <param name="destination">The destination string to validate</param>
        /// <param name="destinationType">The type of destination (email, webhook, phone)</param>
        /// <param name="allowNullOrEmpty">Whether to allow null or empty strings</param>
        /// <returns>The validated destination string</returns>
        /// <exception cref="ArgumentException">Thrown when destination format is invalid</exception>
        public static string ValidateDestination(string destination, DestinationType destinationType, bool allowNullOrEmpty = false)
        {
            return destinationType switch
            {
                DestinationType.Email => ValidateEmail(destination, allowNullOrEmpty),
                DestinationType.Webhook => ValidateWebhookUrl(destination, allowNullOrEmpty),
                DestinationType.Phone => ValidatePhoneNumber(destination, allowNullOrEmpty),
                _ => throw new ArgumentOutOfRangeException(nameof(destinationType), destinationType, "Unknown destination type")
            };
        }

        /// <summary>
        /// Checks if a hostname or IP address is a local or private address (SSRF protection).
        /// </summary>
        /// <param name="host">The hostname or IP address to check</param>
        /// <returns>True if the address is local or private</returns>
        private static bool IsLocalOrPrivateAddress(string host)
        {
            if (string.IsNullOrEmpty(host))
                return false;

            // Check for localhost variations
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                host.Equals("127.0.0.1", StringComparison.Ordinal) ||
                host.Equals("::1", StringComparison.Ordinal) ||
                host.StartsWith("127.", StringComparison.Ordinal) ||
                host.Equals("0.0.0.0", StringComparison.Ordinal))
            {
                return true;
            }

            // Try to parse as IP address
            if (IPAddress.TryParse(host, out var ipAddress))
            {
                // Check for private IP ranges
                return IsPrivateIpAddress(ipAddress);
            }

            // For hostnames, we can't definitively determine if they resolve to private IPs
            // So we allow them and let the actual HTTP call handle it
            return false;
        }

        /// <summary>
        /// Checks if an IP address is in a private range.
        /// </summary>
        /// <param name="ipAddress">The IP address to check</param>
        /// <returns>True if the IP is private</returns>
        private static bool IsPrivateIpAddress(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // IPv6 private ranges
                if (ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal)
                    return true;

                // Check for unique local addresses (fc00::/7)
                var bytes = ipAddress.GetAddressBytes();
                if (bytes.Length >= 2 && (bytes[0] & 0xFE) == 0xFC)
                    return true;
            }
            else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // IPv4 private ranges
                var bytes = ipAddress.GetAddressBytes();
                if (bytes.Length == 4)
                {
                    byte[] address = bytes;

                    // 10.0.0.0/8
                    if (address[0] == 10)
                        return true;

                    // 172.16.0.0/12
                    if (address[0] == 172 && address[1] >= 16 && address[1] <= 31)
                        return true;

                    // 192.168.0.0/16
                    if (address[0] == 192 && address[1] == 168)
                        return true;

                    // 127.0.0.0/8 (loopback)
                    if (address[0] == 127)
                        return true;

                    // 169.254.0.0/16 (link-local)
                    if (address[0] == 169 && address[1] == 254)
                        return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Types of notification destinations
    /// </summary>
    public enum DestinationType
    {
        Email,
        Webhook,
        Phone
    }
}