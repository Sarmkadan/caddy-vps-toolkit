#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
}
