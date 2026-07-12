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
    /// <summary>
    /// Repository for managing UpstreamPool data in the SQLite database.
    /// </summary>
    public sealed class UpstreamPoolRepository : LoadBalancing.IUpstreamPoolRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpstreamPoolRepository"/> class.
        /// Configures the database connection and ensures the schema is initialized.
        /// </summary>
        public UpstreamPoolRepository()
        {
            _connectionString = $"Data Source={AppConstants.DatabasePath};Version=3;";
            InitializeDatabase();
        }

        /// <summary>
        /// Retrieves an <see cref="UpstreamPool"/> by its unique identifier.
        /// </summary>
        /// <param name="poolId">The unique identifier of the upstream pool.</param>
        /// <returns>The <see cref="UpstreamPool"/> if found; otherwise, null.</returns>
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

        /// <summary>
        /// Retrieves all <see cref="UpstreamPool"/>s associated with a specific service.
        /// </summary>
        /// <param name="serviceId">The unique identifier of the service.</param>
        /// <returns>A list of <see cref="UpstreamPool"/>s.</returns>
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

        /// <summary>
        /// Retrieves all <see cref="UpstreamPool"/>s from the database.
        /// </summary>
        /// <returns>A list of all available <see cref="UpstreamPool"/>s.</returns>
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

        /// <summary>
        /// Adds a new <see cref="UpstreamPool"/> to the database.
        /// </summary>
        /// <param name="pool">The <see cref="UpstreamPool"/> to add.</param>
        /// <returns>The unique identifier of the added <see cref="UpstreamPool"/>.</returns>
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

        /// <summary>
        /// Updates an existing <see cref="UpstreamPool"/> in the database.
        /// </summary>
        /// <param name="pool">The <see cref="UpstreamPool"/> to update.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
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

        /// <summary>
        /// Deletes an <see cref="UpstreamPool"/> from the database by its unique identifier.
        /// </summary>
        /// <param name="poolId">The unique identifier of the upstream pool to delete.</param>
        /// <returns>True if the pool was successfully deleted; otherwise, false.</returns>
        public async Task<bool> DeleteAsync(string poolId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM UpstreamPools WHERE Id = @id";
            command.Parameters.AddWithValue("@id", poolId);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        /// <summary>
        /// Checks if an <see cref="UpstreamPool"/> exists in the database.
        /// </summary>
        /// <param name="poolId">The unique identifier of the upstream pool.</param>
        /// <returns>True if the pool exists; otherwise, false.</returns>
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
