// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CaddyVpsToolkit.Auditing
{
    /// <summary>
    /// Audit log entry for tracking important operations
    /// </summary>
    public class AuditLogEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Action { get; set; }
        public string Actor { get; set; }
        public string Target { get; set; }
        public string Result { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// Audit log service for recording operations
    /// </summary>
    public interface IAuditLog
    {
        Task LogAsync(string action, string actor, string target, string result, Dictionary<string, object> details = null);
        Task<List<AuditLogEntry>> GetEntriesAsync(DateTime? from = null, DateTime? to = null);
    }

    /// <summary>
    /// File-based audit log implementation
    /// </summary>
    public class FileAuditLog : IAuditLog
    {
        private readonly string _logPath;
        private readonly List<AuditLogEntry> _entries = new();
        private readonly object _lockObject = new();

        public FileAuditLog(string logPath)
        {
            _logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        public async Task LogAsync(string action, string actor, string target, string result, Dictionary<string, object> details = null)
        {
            var entry = new AuditLogEntry
            {
                Action = action,
                Actor = actor,
                Target = target,
                Result = result,
                Details = details ?? new Dictionary<string, object>()
            };

            lock (_lockObject)
            {
                _entries.Add(entry);
                AppendToFile(entry);
            }
        }

        public async Task<List<AuditLogEntry>> GetEntriesAsync(DateTime? from = null, DateTime? to = null)
        {
            lock (_lockObject)
            {
                var query = _entries.AsEnumerable();

                if (from.HasValue)
                    query = query.Where(e => e.Timestamp >= from.Value);

                if (to.HasValue)
                    query = query.Where(e => e.Timestamp <= to.Value);

                return query.ToList();
            }
        }

        private void AppendToFile(AuditLogEntry entry)
        {
            try
            {
                var json = JsonSerializer.Serialize(entry);
                File.AppendAllText(_logPath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Log the error instead of silently failing
                System.Console.WriteLine($"Failed to write to audit log: {ex.Message}");
            }
        }

        /// <summary>
        /// Get audit summary for action types
        /// </summary>
        public Dictionary<string, int> GetActionSummary()
        {
            lock (_lockObject)
            {
                return _entries
                    .GroupBy(e => e.Action)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }

        /// <summary>
        /// Get entries by actor
        /// </summary>
        public List<AuditLogEntry> GetEntriesByActor(string actor)
        {
            lock (_lockObject)
            {
                return _entries
                    .Where(e => e.Actor == actor)
                    .ToList();
            }
        }
    }
}
