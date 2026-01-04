// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CaddyVpsToolkit.Formatters
{
    /// <summary>
    /// Base interface for output formatters.
    /// Allows multiple output formats (JSON, CSV, table, XML) for the same data.
    /// </summary>
    public interface IOutputFormatter
    {
        string Format<T>(List<T> items);
        string Format<T>(T item);
    }

    /// <summary>
    /// Table formatter for console output with aligned columns
    /// </summary>
    public class TableFormatter : IOutputFormatter
    {
        private readonly int[] _columnWidths;
        private readonly string[] _headers;

        public TableFormatter(params string[] headers)
        {
            _headers = headers;
            _columnWidths = new int[headers.Length];
            for (int i = 0; i < headers.Length; i++)
                _columnWidths[i] = headers[i].Length;
        }

        public string Format<T>(List<T> items)
        {
            if (items == null || items.Count == 0)
                return "No items";

            var lines = new List<string>();

            // Header
            lines.Add(FormatRow(_headers));
            lines.Add(new string('-', _headers.Length * 20));

            // Rows
            foreach (var item in items)
            {
                var values = GetPropertyValues(item);
                lines.Add(FormatRow(values));
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string Format<T>(T item)
        {
            if (item == null)
                return "No item";

            var values = GetPropertyValues(item);
            return FormatRow(values);
        }

        private string FormatRow(string[] values)
        {
            var parts = new List<string>();
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i] ?? "";
                parts.Add(value.PadRight(20));
            }
            return string.Concat(parts);
        }

        private string[] GetPropertyValues<T>(T item)
        {
            var values = new List<string>();
            var props = typeof(T).GetProperties();

            foreach (var prop in props)
            {
                var value = prop.GetValue(item);
                values.Add(value?.ToString() ?? "");
            }

            return values.ToArray();
        }
    }

    /// <summary>
    /// CSV formatter for Excel/spreadsheet compatibility
    /// </summary>
    public class CsvFormatter : IOutputFormatter
    {
        public string Format<T>(List<T> items)
        {
            if (items == null || items.Count == 0)
                return "";

            var lines = new List<string>();

            // Header
            var headerProps = typeof(T).GetProperties();
            var headers = new List<string>();
            foreach (var prop in headerProps)
                headers.Add(EscapeCsv(prop.Name));
            lines.Add(string.Join(",", headers));

            // Rows
            foreach (var item in items)
            {
                var values = new List<string>();
                foreach (var prop in headerProps)
                {
                    var value = prop.GetValue(item)?.ToString() ?? "";
                    values.Add(EscapeCsv(value));
                }
                lines.Add(string.Join(",", values));
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string Format<T>(T item)
        {
            if (item == null)
                return "";

            return Format(new List<T> { item });
        }

        private string EscapeCsv(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }

    /// <summary>
    /// JSON formatter for API responses
    /// </summary>
    public class JsonFormatter : IOutputFormatter
    {
        public string Format<T>(List<T> items)
        {
            if (items == null)
                items = new List<T>();

            return JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
        }

        public string Format<T>(T item)
        {
            if (item == null)
                return "null";

            return JsonSerializer.Serialize(item, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    /// <summary>
    /// Plain text key-value formatter
    /// </summary>
    public class TextFormatter : IOutputFormatter
    {
        public string Format<T>(List<T> items)
        {
            if (items == null || items.Count == 0)
                return "No items";

            var lines = new List<string>();
            int index = 1;

            foreach (var item in items)
            {
                lines.Add($"--- Item {index} ---");
                lines.AddRange(FormatObject(item));
                lines.Add("");
                index++;
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string Format<T>(T item)
        {
            if (item == null)
                return "No item";

            var lines = FormatObject(item);
            return string.Join(Environment.NewLine, lines);
        }

        private List<string> FormatObject<T>(T item)
        {
            var lines = new List<string>();
            var props = typeof(T).GetProperties();

            foreach (var prop in props)
            {
                var value = prop.GetValue(item)?.ToString() ?? "(null)";
                lines.Add($"{prop.Name}: {value}");
            }

            return lines;
        }
    }
}
