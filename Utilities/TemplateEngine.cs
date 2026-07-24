#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Text;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Exception thrown when template rendering encounters unresolved variables in strict mode.
    /// </summary>
    public sealed class TemplateVariableMissingException : Exception
    {
        /// <summary>
        /// Gets the collection of missing variable names.
        /// </summary>
        public IReadOnlyCollection<string> MissingVariables { get; }

        /// <summary>
        /// Initializes a new instance of the TemplateVariableMissingException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="missingVariables">The collection of missing variable names.</param>
        public TemplateVariableMissingException(string message, IEnumerable<string> missingVariables)
            : base(message)
        {
            MissingVariables = new List<string>(missingVariables ?? Array.Empty<string>());
        }
    }

    /// <summary>
    /// Simple template engine for string substitution.
    /// Uses {{variable}} syntax for placeholder replacement.
    ///
    /// <para>Strict Mode (default):</para>
    /// <para>• Throws TemplateVariableMissingException when unresolved variables are encountered</para>
    /// <para>• Allows escaping literal braces using \{{ and \}} syntax</para>
    ///
    /// <para>Lenient Mode (opt-out):</para>
    /// <para>• Silently leaves unresolved placeholders as-is (backward compatible behavior)</para>
    /// <para>• Does not support escaping literal braces</para>
    /// </summary>
    public sealed class TemplateEngine
    {
        private readonly Dictionary<string, object> _variables;
        private readonly bool _strictMode;

        /// <summary>
        /// Gets or sets a value indicating whether strict mode is enabled.
        /// When true (default), unresolved variables throw TemplateVariableMissingException.
        /// When false (lenient mode), unresolved variables remain as-is in the output.
        /// </summary>
        public bool StrictMode { get; set; } = true;

        public TemplateEngine()
            : this(new Dictionary<string, object>(), strictMode: true)
        {
        }

        /// <summary>
        /// Initializes a new TemplateEngine instance with strict mode enabled.
        /// </summary>
        /// <param name="variables">Initial variables dictionary.</param>
        public TemplateEngine(Dictionary<string, object> variables)
            : this(variables, strictMode: true)
        {
        }

        /// <summary>
        /// Initializes a new TemplateEngine instance.
        /// </summary>
        /// <param name="variables">Initial variables dictionary.</param>
        /// <param name="strictMode">Whether to enable strict mode (default: true).</param>
        public TemplateEngine(Dictionary<string, object> variables, bool strictMode)
        {
            _variables = variables ?? new Dictionary<string, object>();
            _strictMode = strictMode;
        }

        /// <summary>
        /// Sets a variable value.
        /// </summary>
        /// <param name="key">Variable name (required).</param>
        /// <param name="value">Variable value.</param>
        /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
        public void Set(string key, object value)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            _variables[key] = value;
        }

        /// <summary>
        /// Gets a variable value.
        /// </summary>
        /// <param name="key">Variable name.</param>
        /// <returns>The variable value, or null if not found.</returns>
        public object Get(string key)
        {
            return _variables.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Render template with variable substitution.
        ///
        /// <para>Placeholder syntax: {{variableName}}</para>
        /// <para>Escaping literal braces: \{{ and \}} (only in strict mode)</para>
        /// <para>Strict mode behavior: Throws TemplateVariableMissingException for unresolved variables</para>
        /// <para>Lenient mode behavior: Leaves unresolved placeholders unchanged</para>
        /// </summary>
        /// <param name="template">Template string to render.</param>
        /// <returns>Rendered string with all placeholders substituted.</returns>
        /// <exception cref="ArgumentNullException">Thrown when template is null.</exception>
        /// <exception cref="TemplateVariableMissingException">
        /// Thrown in strict mode when unresolved variables are encountered.
        /// Contains list of all missing variable names in the MissingVariables property.
        /// </exception>
        public string Render(string template)
        {
            ArgumentNullException.ThrowIfNull(template);

            if (template.Length == 0)
            {
                return template;
            }

            var unresolvedVariables = new HashSet<string>(StringComparer.Ordinal);
            var result = new StringBuilder(template.Length);
            var remaining = template.AsSpan();

            while (!remaining.IsEmpty)
            {
                // Find next placeholder or escape sequence
                var placeholderIndex = remaining.IndexOf("{{", StringComparison.Ordinal);
                var escapeIndex = remaining.IndexOf("\\{{", StringComparison.Ordinal);

                // Handle escape sequences first (only in strict mode)
                if (StrictMode && escapeIndex >= 0 && (placeholderIndex < 0 || escapeIndex < placeholderIndex))
                {
                    // Escaped literal brace: \{{ → {{
                    result.Append(remaining[..escapeIndex]);
                    result.Append('{');
                    remaining = remaining[(escapeIndex + 2)..];
                    continue;
                }

                // No more placeholders found
                if (placeholderIndex < 0)
                {
                    result.Append(remaining);
                    break;
                }

                // Append text before placeholder
                result.Append(remaining[..placeholderIndex]);
                remaining = remaining[(placeholderIndex + 2)..];

                // Extract variable name
                var nameEnd = remaining.IndexOf('}');
                if (nameEnd < 0)
                {
                    // Malformed placeholder, leave as-is
                    result.Append("{{");
                    continue;
                }

                var variableName = remaining[..nameEnd].ToString();

                // Check if we have a closing brace to consume
                var closingBraceEnd = nameEnd + 1;
                if (closingBraceEnd < remaining.Length && remaining[closingBraceEnd] == '}')
                {
                    // Consume the closing brace
                    remaining = remaining[(closingBraceEnd + 1)..];
                }
                else
                {
                    // Malformed placeholder, leave as-is
                    result.Append("{{").Append(variableName).Append("}}");
                    continue;
                }

                // Check if variable exists
                if (_variables.TryGetValue(variableName, out var value))
                {
                    result.Append(value?.ToString() ?? string.Empty);
                }
                else
                {
                    unresolvedVariables.Add(variableName);

                    if (_strictMode)
                    {
                        // In strict mode, leave placeholder as-is for now
                        // We'll throw after collecting all unresolved variables
                        result.Append("{{").Append(variableName).Append("}}");
                    }
                    else
                    {
                        // In lenient mode, leave placeholder unchanged
                        result.Append("{{").Append(variableName).Append("}}");
                    }
                }
            }

            // Throw if any unresolved variables found in strict mode
            if (_strictMode && unresolvedVariables.Count > 0)
            {
                throw new TemplateVariableMissingException(
                    $"Template contains {unresolvedVariables.Count} unresolved variable(s): {string.Join(", ", unresolvedVariables)}",
                    unresolvedVariables);
            }

            return result.ToString();
        }

        /// <summary>
        /// Render template with inline dictionary and strict mode enabled.
        /// </summary>
        /// <param name="template">Template string to render.</param>
        /// <param name="variables">Variables dictionary.</param>
        /// <returns>Rendered string with all placeholders substituted.</returns>
        /// <exception cref="TemplateVariableMissingException">
        /// Thrown when unresolved variables are encountered.
        /// </exception>
        public static string Render(string template, Dictionary<string, object> variables)
        {
            var engine = new TemplateEngine(variables, strictMode: true);
            return engine.Render(template);
        }
    }
}