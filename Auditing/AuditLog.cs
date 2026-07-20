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
using CaddyVpsToolkit.Data;
using CaddyVpsToolkit.Results;

namespace CaddyVpsToolkit.Auditing
{
    /// <summary>
    /// Audit log entry for tracking important operations
    /// </summary>
    public sealed class AuditLogEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// Audit log service for recording operations
    /// </summary>
    public interface IAuditLog
    {
        Task LogAsync(string action, string actor, string target, string result, Dictionary<string, object> details = null);
        Task<List<AuditLogEntry>> GetEntriesAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>
        /// Query audit log entries with filtering by time range, actor, and action
        /// </summary>
        /// <param name="fromUtc">Start of time range (inclusive)</param>
        /// <param name="toUtc">End of time range (inclusive)</param>
        /// <param name="actor">Filter by actor name (optional)</param>
        /// <param name="action">Filter by action name (optional)</param>
        /// <returns>List of matching audit log entries</returns>
        Task<List<AuditLogEntry>> QueryAsync(DateTime? fromUtc = null, DateTime? toUtc = null, string actor = null, string action = null);

        /// <summary>
        /// Query audit log entries with filtering and pagination support
        /// </summary>
        /// <param name="fromUtc">Start of time range (inclusive)</param>
        /// <param name="toUtc">End of time range (inclusive)</param>
        /// <param name="actor">Filter by actor name (optional)</param>
        /// <param name="action">Filter by action name (optional)</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated result with matching audit log entries</returns>
        Task<PaginatedResult<AuditLogEntry>> QueryAsync(
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            string actor = null,
            string action = null,
            int page = 1,
            int pageSize = 10);
    }

    /// <summary>
    /// File-based audit log implementation
    /// </summary>
    public sealed class FileAuditLog : IAuditLog
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

        /// <summary>
        /// Query audit log entries with filtering by time range, actor, and action
        /// </summary>
        /// <param name="fromUtc">Start of time range (inclusive)</param>
        /// <param name="toUtc">End of time range (inclusive)</param>
        /// <param name="actor">Filter by actor name (optional)</param>
        /// <param name="action">Filter by action name (optional)</param>
        /// <returns>List of matching audit log entries</returns>
        public async Task<List<AuditLogEntry>> QueryAsync(DateTime? fromUtc = null, DateTime? toUtc = null, string actor = null, string action = null)
        {
            lock (_lockObject)
            {
                var query = _entries.AsEnumerable();

                if (fromUtc.HasValue)
                    query = query.Where(e => e.Timestamp >= fromUtc.Value);

                if (toUtc.HasValue)
                    query = query.Where(e => e.Timestamp <= toUtc.Value);

                if (!string.IsNullOrEmpty(actor))
                    query = query.Where(e => e.Actor == actor);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(e => e.Action == action);

                return query.ToList();
            }
        }

        /// <summary>
        /// Query audit log entries with filtering and pagination support
        /// </summary>
        /// <param name="fromUtc">Start of time range (inclusive)</param>
        /// <param name="toUtc">End of time range (inclusive)</param>
        /// <param name="actor">Filter by actor name (optional)</param>
        /// <param name="action">Filter by action name (optional)</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Paginated result with matching audit log entries</returns>
        public async Task<PaginatedResult<AuditLogEntry>> QueryAsync(
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            string actor = null,
            string action = null,
            int page = 1,
            int pageSize = 10)
        {
            lock (_lockObject)
            {
                var query = _entries.AsEnumerable();

                if (fromUtc.HasValue)
                    query = query.Where(e => e.Timestamp >= fromUtc.Value);

                if (toUtc.HasValue)
                    query = query.Where(e => e.Timestamp <= toUtc.Value);

                if (!string.IsNullOrEmpty(actor))
                    query = query.Where(e => e.Actor == actor);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(e => e.Action == action);

                return PaginationHelper.Paginate(query, page, pageSize);
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
