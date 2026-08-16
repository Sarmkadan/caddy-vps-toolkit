#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
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

    internal interface ITemplateSegment
    {
        void Render(StringBuilder builder, Dictionary<string, object> variables, bool strictMode, HashSet<string> unresolved);
    }

    internal sealed class LiteralSegment : ITemplateSegment
    {
        private readonly string _text;
        public LiteralSegment(string text) => _text = text;
        public void Render(StringBuilder builder, Dictionary<string, object> variables, bool strictMode, HashSet<string> unresolved) => builder.Append(_text);
    }

    internal sealed class VariableSegment : ITemplateSegment
    {
        private readonly string _name;
        private readonly string _originalPlaceholder;
        public VariableSegment(string name, string originalPlaceholder)
        {
            _name = name;
            _originalPlaceholder = originalPlaceholder;
        }

        public void Render(StringBuilder builder, Dictionary<string, object> variables, bool strictMode, HashSet<string> unresolved)
        {
            if (variables.TryGetValue(_name, out var value))
            {
                builder.Append(value?.ToString() ?? string.Empty);
            }
            else
            {
                unresolved.Add(_name);
                builder.Append(_originalPlaceholder);
            }
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

        // Cache key is a hash of the template string combined with the strict‑mode flag.
        // Using a hash reduces memory pressure compared to storing the full template string as a key.
        private static readonly ConcurrentDictionary<(string hash, bool strict), List<ITemplateSegment>> _templateCache = new();

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
            Set(key, value, null);
        }

        /// <summary>
        /// Sets a variable value with an optional validation hook.
        /// </summary>
        /// <param name="key">Variable name (required).</param>
        /// <param name="value">Variable value.</param>
        /// <param name="validator">Optional validation function (returns true if valid).</param>
        /// <exception cref="ArgumentException">Thrown when key is null or empty, or validation fails.</exception>
        public void Set(string key, object value, Func<object, bool>? validator)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            if (validator != null && !validator(value))
            {
                throw new ArgumentException($"Validation failed for variable '{key}'");
            }

            _variables[key] = value;
        }

        /// <summary>
        /// Sets a variable value, sanitized for Caddyfile context.
        /// </summary>
        /// <param name="key">Variable name (required).</param>
        /// <param name="value">Variable value to sanitize.</param>
        /// <exception cref="ArgumentException">Thrown when key is null or empty, or value contains invalid characters.</exception>
        public void SetCaddyValue(string key, string value)
        {
            Set(key, TemplateValueSanitizer.SanitizeCaddyValue(value));
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

        private static List<ITemplateSegment> Parse(string template, bool strictMode)
        {
            var segments = new List<ITemplateSegment>();
            var remaining = template.AsSpan();

            while (!remaining.IsEmpty)
            {
                var placeholderIndex = remaining.IndexOf("{{", StringComparison.Ordinal);
                var escapeIndex = remaining.IndexOf("\\{{", StringComparison.Ordinal);

                if (strictMode && escapeIndex >= 0 && (placeholderIndex < 0 || escapeIndex < placeholderIndex))
                {
                    segments.Add(new LiteralSegment(remaining[..escapeIndex].ToString() + "{"));
                    remaining = remaining[(escapeIndex + 2)..];
                    continue;
                }

                if (placeholderIndex < 0)
                {
                    segments.Add(new LiteralSegment(remaining.ToString()));
                    break;
                }

                segments.Add(new LiteralSegment(remaining[..placeholderIndex].ToString()));
                remaining = remaining[(placeholderIndex + 2)..];

                var nameEnd = remaining.IndexOf('}');
                if (nameEnd < 0)
                {
                    segments.Add(new LiteralSegment("{{"));
                    continue;
                }

                var variableName = remaining[..nameEnd].ToString();
                var closingBraceEnd = nameEnd + 1;

                if (closingBraceEnd < remaining.Length && remaining[closingBraceEnd] == '}')
                {
                    segments.Add(new VariableSegment(variableName, "{{" + variableName + "}}"));
                    remaining = remaining[(closingBraceEnd + 1)..];
                }
                else
                {
                    segments.Add(new LiteralSegment("{{" + variableName));
                }
            }
            return segments;
        }

        /// <summary>
        /// Compute a stable SHA‑256 hash for a template string.
        /// The hash is returned as a lower‑case hexadecimal string.
        /// </summary>
        private static string ComputeHash(string input)
        {
            // SHA256 is deterministic and does not depend on process‑wide randomization.
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
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
                return template;

            // Use a hash of the template as the cache key.
            var hash = ComputeHash(template);
            var cacheKey = (hash, _strictMode);

            var segments = _templateCache.GetOrAdd(cacheKey, _ => Parse(template, _strictMode));

            var unresolvedVariables = new HashSet<string>(StringComparer.Ordinal);
            var result = new StringBuilder(template.Length);

            foreach (var segment in segments)
            {
                segment.Render(result, _variables, _strictMode, unresolvedVariables);
            }

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
