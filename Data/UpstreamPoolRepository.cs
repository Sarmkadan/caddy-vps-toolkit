#nullable enable
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text.Json;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Data
{
    public sealed class UpstreamPoolRepository : LoadBalancing.IUpstreamPoolRepository
    {
        private readonly string _connectionString;

        public UpstreamPoolRepository()
        {
            _connectionString = $"Data Source={AppConstants.DatabasePath};Version=3;";
            InitializeDatabase();
        }

        public async Task<UpstreamPool?> GetByIdAsync(string poolId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM UpstreamPools WHERE Id = @id";
            command.Parameters.AddWithValue("@id", poolId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapReaderToPool(reader);
            
            return null;
        }

        public async Task<List<UpstreamPool>> GetByServiceIdAsync(string serviceId)
        {
            var pools = new List<UpstreamPool>();
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM UpstreamPools WHERE ServiceId = @serviceId";
            command.Parameters.AddWithValue("@serviceId", serviceId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                pools.Add(MapReaderToPool(reader));
            
            return pools;
        }

        public async Task<List<UpstreamPool>> GetAllAsync()
        {
            var pools = new List<UpstreamPool>();
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM UpstreamPools";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                pools.Add(MapReaderToPool(reader));
            
            return pools;
        }

        public async Task<string> AddAsync(UpstreamPool pool)
        {
            pool.Validate();
            if (string.IsNullOrEmpty(pool.Id))
                pool.Id = Guid.NewGuid().ToString();

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO UpstreamPools (Id, Name, ServiceId, Strategy, Servers, PassiveHealthEnabled, 
                    ActiveHealthEnabled, HealthCheckIntervalSeconds, UnhealthyThreshold, HealthyThreshold, 
                    MaxRetries, RetryDurationSeconds, StickyCookieName, HealthProbePath, IsEnabled, 
                    CreatedAt, UpdatedAt)
                VALUES (@id, @name, @serviceId, @strategy, @servers, @passiveHealth, 
                    @activeHealth, @healthInterval, @unhealthyThresh, @healthyThresh, 
                    @maxRetries, @retryDuration, @stickyCookie, @healthProbe, @isEnabled, 
                    @createdAt, @updatedAt)";

            command.Parameters.AddWithValue("@id", pool.Id);
            command.Parameters.AddWithValue("@name", pool.Name);
            command.Parameters.AddWithValue("@serviceId", pool.ServiceId);
            command.Parameters.AddWithValue("@strategy", (int)pool.Strategy);
            command.Parameters.AddWithValue("@servers", JsonSerializer.Serialize(pool.Servers));
            command.Parameters.AddWithValue("@passiveHealth", pool.PassiveHealthEnabled ? 1 : 0);
            command.Parameters.AddWithValue("@activeHealth", pool.ActiveHealthEnabled ? 1 : 0);
            command.Parameters.AddWithValue("@healthInterval", pool.HealthCheckIntervalSeconds);
            command.Parameters.AddWithValue("@unhealthyThresh", pool.UnhealthyThreshold);
            command.Parameters.AddWithValue("@healthyThresh", pool.HealthyThreshold);
            command.Parameters.AddWithValue("@maxRetries", pool.MaxRetries);
            command.Parameters.AddWithValue("@retryDuration", pool.RetryDurationSeconds);
            command.Parameters.AddWithValue("@stickyCookie", pool.StickyCookieName ?? "");
            command.Parameters.AddWithValue("@healthProbe", pool.HealthProbePath ?? "");
            command.Parameters.AddWithValue("@isEnabled", pool.IsEnabled ? 1 : 0);
            command.Parameters.AddWithValue("@createdAt", pool.CreatedAt.Ticks);
            command.Parameters.AddWithValue("@updatedAt", pool.UpdatedAt.Ticks);

            await command.ExecuteNonQueryAsync();
            return pool.Id;
        }

        public async Task<bool> UpdateAsync(UpstreamPool pool)
        {
            pool.Validate();
            pool.UpdatedAt = DateTime.UtcNow;

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE UpstreamPools SET Name = @name, ServiceId = @serviceId, Strategy = @strategy, 
                    Servers = @servers, PassiveHealthEnabled = @passiveHealth, ActiveHealthEnabled = @activeHealth, 
                    HealthCheckIntervalSeconds = @healthInterval, UnhealthyThreshold = @unhealthyThresh, 
                    HealthyThreshold = @healthyThresh, MaxRetries = @maxRetries, RetryDurationSeconds = @retryDuration, 
                    StickyCookieName = @stickyCookie, HealthProbePath = @healthProbe, IsEnabled = @isEnabled, 
                    UpdatedAt = @updatedAt
                WHERE Id = @id";

            command.Parameters.AddWithValue("@id", pool.Id);
            command.Parameters.AddWithValue("@name", pool.Name);
            command.Parameters.AddWithValue("@serviceId", pool.ServiceId);
            command.Parameters.AddWithValue("@strategy", (int)pool.Strategy);
            command.Parameters.AddWithValue("@servers", JsonSerializer.Serialize(pool.Servers));
            command.Parameters.AddWithValue("@passiveHealth", pool.PassiveHealthEnabled ? 1 : 0);
            command.Parameters.AddWithValue("@activeHealth", pool.ActiveHealthEnabled ? 1 : 0);
            command.Parameters.AddWithValue("@healthInterval", pool.HealthCheckIntervalSeconds);
            command.Parameters.AddWithValue("@unhealthyThresh", pool.UnhealthyThreshold);
            command.Parameters.AddWithValue("@healthyThresh", pool.HealthyThreshold);
            command.Parameters.AddWithValue("@maxRetries", pool.MaxRetries);
            command.Parameters.AddWithValue("@retryDuration", pool.RetryDurationSeconds);
            command.Parameters.AddWithValue("@stickyCookie", pool.StickyCookieName ?? "");
            command.Parameters.AddWithValue("@healthProbe", pool.HealthProbePath ?? "");
            command.Parameters.AddWithValue("@isEnabled", pool.IsEnabled ? 1 : 0);
            command.Parameters.AddWithValue("@updatedAt", pool.UpdatedAt.Ticks);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(string poolId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM UpstreamPools WHERE Id = @id";
            command.Parameters.AddWithValue("@id", poolId);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ExistsAsync(string poolId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM UpstreamPools WHERE Id = @id";
            command.Parameters.AddWithValue("@id", poolId);
            var result = await command.ExecuteScalarAsync();
            return result is not null && (long)result > 0;
        }

        private UpstreamPool MapReaderToPool(System.Data.Common.DbDataReader reader)
        {
            var serversJson = reader["Servers"].ToString();
            var servers = string.IsNullOrEmpty(serversJson) 
                ? new List<UpstreamServer>() 
                : JsonSerializer.Deserialize<List<UpstreamServer>>(serversJson) ?? new List<UpstreamServer>();

            return new UpstreamPool
            {
                Id = reader["Id"].ToString() ?? "",
                Name = reader["Name"].ToString() ?? "",
                ServiceId = reader["ServiceId"].ToString() ?? "",
                Strategy = (LoadBalancingStrategy)(long)reader["Strategy"],
                Servers = servers,
                PassiveHealthEnabled = (long)reader["PassiveHealthEnabled"] > 0,
                ActiveHealthEnabled = (long)reader["ActiveHealthEnabled"] > 0,
                HealthCheckIntervalSeconds = (int)(long)reader["HealthCheckIntervalSeconds"],
                UnhealthyThreshold = (int)(long)reader["UnhealthyThreshold"],
                HealthyThreshold = (int)(long)reader["HealthyThreshold"],
                MaxRetries = (int)(long)reader["MaxRetries"],
                RetryDurationSeconds = (int)(long)reader["RetryDurationSeconds"],
                StickyCookieName = reader["StickyCookieName"].ToString(),
                HealthProbePath = reader["HealthProbePath"].ToString() ?? "",
                IsEnabled = (long)reader["IsEnabled"] > 0,
                CreatedAt = new DateTime((long)reader["CreatedAt"]),
                UpdatedAt = new DateTime((long)reader["UpdatedAt"])
            };
        }

        private void InitializeDatabase()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS UpstreamPools (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    ServiceId TEXT NOT NULL,
                    Strategy INTEGER,
                    Servers TEXT,
                    PassiveHealthEnabled INTEGER,
                    ActiveHealthEnabled INTEGER,
                    HealthCheckIntervalSeconds INTEGER,
                    UnhealthyThreshold INTEGER,
                    HealthyThreshold INTEGER,
                    MaxRetries INTEGER,
                    RetryDurationSeconds INTEGER,
                    StickyCookieName TEXT,
                    HealthProbePath TEXT,
                    IsEnabled INTEGER,
                    CreatedAt INTEGER,
                    UpdatedAt INTEGER
                )";
            command.ExecuteNonQuery();
        }
    }
}
