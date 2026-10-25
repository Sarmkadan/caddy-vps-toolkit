#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace CaddyVpsToolkit.Notifications
{
    /// <summary>
    /// Message templates used by <see cref="NotificationService"/>.
    /// Placeholders use the <c>{{variableName}}</c> syntax understood by
    /// <see cref="Utilities.TemplateEngine"/>.
    /// </summary>
    public static class NotificationTemplates
    {
        /// <summary>Status word used when a provider delivery succeeded.</summary>
        public const string StatusSucceeded = "succeeded";

        /// <summary>Status word used when a provider delivery failed.</summary>
        public const string StatusFailed = "failed";

        /// <summary>Error text recorded in the dead-letter queue when a provider reports failure without an exception.</summary>
        public const string DeliveryFailedError = "Delivery failed";

        /// <summary>Logged when a provider send throws. Variables: provider, error.</summary>
        public const string ProviderSendError = "Error sending notification via {{provider}}: {{error}}";

        /// <summary>Logged when a duplicate notification is suppressed. Variables: title.</summary>
        public const string DuplicateSuppressed = "Suppressed duplicate notification: {{title}}";

        /// <summary>Logged after each provider attempt. Variables: provider, status.</summary>
        public const string ProviderSendResult = "Notification sent via {{provider}} ({{status}})";

        /// <summary>Logged for each failed dead-letter entry. Variables: provider, error.</summary>
        public const string DeliveryFailedWarning = "Failed to deliver notification to {{provider}}: {{error}}";

        /// <summary>Console provider title line. Variables: priority, title.</summary>
        public const string ConsoleTitle = "[{{priority}}] {{title}}";

        /// <summary>Console provider message line. Variables: message.</summary>
        public const string ConsoleMessage = " {{message}}";
    }
}
