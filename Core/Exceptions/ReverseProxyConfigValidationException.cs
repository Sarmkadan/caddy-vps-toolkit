// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Thrown when a ReverseProxyConfig fails validation before it is rendered for Caddy
// =============================================================================

#nullable enable

using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Core.Exceptions
{
    /// <summary>
    /// Thrown when a <c>ReverseProxyConfig</c> fails validation. Carries every problem found,
    /// not just the first, so all issues can be fixed in one pass.
    /// </summary>
    public sealed class ReverseProxyConfigValidationException : CaddyVpsException
    {
        /// <summary>
        /// Gets the human-readable validation problems, one entry per rule violation.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReverseProxyConfigValidationException"/> class.
        /// </summary>
        /// <param name="errors">The validation problems that were found. Must contain at least one entry.</param>
        public ReverseProxyConfigValidationException(IReadOnlyList<string> errors)
            : base(BuildMessage(errors), "REVERSE_PROXY_CONFIG_INVALID", errors)
        {
            Errors = errors;
        }

        private static string BuildMessage(IReadOnlyList<string> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);
            return $"ReverseProxyConfig is invalid ({errors.Count} problem(s)):\n- " + string.Join("\n- ", errors);
        }
    }
}
