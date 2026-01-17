#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Simple template engine for string substitution.
    /// Uses {{variable}} syntax for placeholder replacement.
    /// </summary>
    public sealed class TemplateEngine
    {
        private readonly Dictionary<string, object> _variables;

        public TemplateEngine()
        {
            _variables = new Dictionary<string, object>();
        }

        public TemplateEngine(Dictionary<string, object> variables)
        {
            _variables = variables ?? new Dictionary<string, object>();
        }

        public void Set(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key required", nameof(key));

            _variables[key] = value;
        }

        public object Get(string key)
        {
            return _variables.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Render template with variable substitution
        /// </summary>
        public string Render(string template)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            return Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
            {
                var key = match.Groups[1].Value;
                return _variables.TryGetValue(key, out var value) ? value?.ToString() ?? "" : match.Value;
            });
        }

        /// <summary>
        /// Render template with inline dictionary
        /// </summary>
        public static string Render(string template, Dictionary<string, object> variables)
        {
            var engine = new TemplateEngine(variables);
            return engine.Render(template);
        }
    }
}
