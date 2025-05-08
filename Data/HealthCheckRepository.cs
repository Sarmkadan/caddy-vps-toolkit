// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Data
{
    /// <summary>
    /// SQLite-based repository for health check results
    /// </summary>
    public class HealthCheckRepository : IHealthCheckRepository
    {
        private readonly string _connectionString;

        public HealthCheckRepository()
        {
            _connectionString = $"Data Source={AppConstants.DatabasePath};Version=3;";
            InitializeDatabase();
        }

        public async Task<HealthCheckResult> GetLatestAsync(string serviceId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT * FROM HealthCheckResults
                    WHERE ServiceId = @serviceId
                    ORDER BY CheckedAt DESC
                    LIMIT 1";
                command.Parameters.AddWithValue("@serviceId", serviceId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        return MapReaderToResult(reader);
                }
            }
            return null;
        }

        public async Task<List<HealthCheckResult>> GetRecentAsync(string serviceId, int hours)
        {
            var results = new List<HealthCheckResult>();
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT * FROM HealthCheckResults
                    WHERE ServiceId = @serviceId AND CheckedAt > @cutoff
                    ORDER BY CheckedAt DESC";
                command.Parameters.AddWithValue("@serviceId", serviceId);
                command.Parameters.AddWithValue("@cutoff", cutoffTime.Ticks);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        results.Add(MapReaderToResult(reader));
                }
            }
            return results;
        }

        public async Task<List<HealthCheckResult>> GetByServiceIdAsync(string serviceId)
        {
            var results = new List<HealthCheckResult>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM HealthCheckResults WHERE ServiceId = @serviceId ORDER BY CheckedAt DESC";
                command.Parameters.AddWithValue("@serviceId", serviceId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        results.Add(MapReaderToResult(reader));
                }
            }
            return results;
        }

        public async Task<string> AddAsync(HealthCheckResult result)
        {
            if (string.IsNullOrEmpty(result.Id))
                result.Id = Guid.NewGuid().ToString();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO HealthCheckResults (Id, ServiceId, IsHealthy, Status, ResponseTimeMs,
                        HttpStatusCode, ErrorMessage, ResponseBody, CheckedAt, ConsecutiveFailures,
                        ConsecutiveSuccesses, CheckType, Endpoint)
                    VALUES (@id, @serviceId, @isHealthy, @status, @responseTime, @httpStatus,
                        @errorMsg, @responseBody, @checkedAt, @consecFail, @consecSuccess, @checkType, @endpoint)";

                command.Parameters.AddWithValue("@id", result.Id);
                command.Parameters.AddWithValue("@serviceId", result.ServiceId);
                command.Parameters.AddWithValue("@isHealthy", result.IsHealthy ? 1 : 0);
                command.Parameters.AddWithValue("@status", (int)result.Status);
                command.Parameters.AddWithValue("@responseTime", result.ResponseTimeMs);
                command.Parameters.AddWithValue("@httpStatus", result.HttpStatusCode);
                command.Parameters.AddWithValue("@errorMsg", result.ErrorMessage ?? "");
                command.Parameters.AddWithValue("@responseBody", result.ResponseBody ?? "");
                command.Parameters.AddWithValue("@checkedAt", result.CheckedAt.Ticks);
                command.Parameters.AddWithValue("@consecFail", result.ConsecutiveFailures);
                command.Parameters.AddWithValue("@consecSuccess", result.ConsecutiveSuccesses);
                command.Parameters.AddWithValue("@checkType", result.CheckType ?? "");
                command.Parameters.AddWithValue("@endpoint", result.Endpoint ?? "");

                await command.ExecuteNonQueryAsync();
            }
            return result.Id;
        }

        public async Task<bool> DeleteOlderThanAsync(DateTime cutoffDate)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM HealthCheckResults WHERE CheckedAt < @cutoff";
                command.Parameters.AddWithValue("@cutoff", cutoffDate.Ticks);
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }

        public async Task<HealthCheckStatistics> GetStatisticsAsync(string serviceId, DateTime from, DateTime to)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT
                        COUNT(*) as TotalChecks,
                        SUM(CASE WHEN IsHealthy = 1 THEN 1 ELSE 0 END) as SuccessfulChecks,
                        SUM(CASE WHEN IsHealthy = 0 THEN 1 ELSE 0 END) as FailedChecks,
                        AVG(ResponseTimeMs) as AvgResponseTime,
                        MAX(ResponseTimeMs) as MaxResponseTime,
                        MIN(ResponseTimeMs) as MinResponseTime
                    FROM HealthCheckResults
                    WHERE ServiceId = @serviceId AND CheckedAt BETWEEN @from AND @to";

                command.Parameters.AddWithValue("@serviceId", serviceId);
                command.Parameters.AddWithValue("@from", from.Ticks);
                command.Parameters.AddWithValue("@to", to.Ticks);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var totalChecks = reader["TotalChecks"] is DBNull ? 0 : (long)reader["TotalChecks"];
                        var successfulChecks = reader["SuccessfulChecks"] is DBNull ? 0 : (long)reader["SuccessfulChecks"];
                        var failedChecks = reader["FailedChecks"] is DBNull ? 0 : (long)reader["FailedChecks"];
                        var avgResponseTime = reader["AvgResponseTime"] is DBNull ? 0 : (long)(double)reader["AvgResponseTime"];

                        return new HealthCheckStatistics
                        {
                            TotalChecks = (int)totalChecks,
                            SuccessfulChecks = (int)successfulChecks,
                            FailedChecks = (int)failedChecks,
                            SuccessRate = totalChecks > 0 ? (double)successfulChecks / totalChecks : 0,
                            AverageResponseTimeMs = (int)avgResponseTime,
                            MaxResponseTimeMs = reader["MaxResponseTime"] is DBNull ? 0 : (int)(long)reader["MaxResponseTime"],
                            MinResponseTimeMs = reader["MinResponseTime"] is DBNull ? 0 : (int)(long)reader["MinResponseTime"]
                        };
                    }
                }
            }

            return new HealthCheckStatistics { TotalChecks = 0, SuccessRate = 0 };
        }

        private HealthCheckResult MapReaderToResult(SQLiteDataReader reader)
        {
            return new HealthCheckResult
            {
                Id = reader["Id"].ToString(),
                ServiceId = reader["ServiceId"].ToString(),
                IsHealthy = (long)reader["IsHealthy"] > 0,
                Status = (HealthCheckStatus)(long)reader["Status"],
                ResponseTimeMs = (int)(long)reader["ResponseTimeMs"],
                HttpStatusCode = (int)(long)reader["HttpStatusCode"],
                ErrorMessage = reader["ErrorMessage"].ToString(),
                ResponseBody = reader["ResponseBody"].ToString(),
                CheckedAt = new DateTime((long)reader["CheckedAt"]),
                ConsecutiveFailures = (int)(long)reader["ConsecutiveFailures"],
                ConsecutiveSuccesses = (int)(long)reader["ConsecutiveSuccesses"],
                CheckType = reader["CheckType"].ToString(),
                Endpoint = reader["Endpoint"].ToString()
            };
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS HealthCheckResults (
                            Id TEXT PRIMARY KEY,
                            ServiceId TEXT NOT NULL,
                            IsHealthy INTEGER,
                            Status INTEGER,
                            ResponseTimeMs INTEGER,
                            HttpStatusCode INTEGER,
                            ErrorMessage TEXT,
                            ResponseBody TEXT,
                            CheckedAt INTEGER,
                            ConsecutiveFailures INTEGER,
                            ConsecutiveSuccesses INTEGER,
                            CheckType TEXT,
                            Endpoint TEXT,
                            FOREIGN KEY(ServiceId) REFERENCES Services(Id)
                        )";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE INDEX IF NOT EXISTS idx_serviceid_checkedat ON HealthCheckResults(ServiceId, CheckedAt)";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Failed to initialize health check table: {ex.Message}", ex);
            }
        }
    }
}
