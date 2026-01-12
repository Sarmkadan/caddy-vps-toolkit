// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;

namespace CaddyVpsToolkit.Data
{
    /// <summary>
    /// SQLite-based repository for application configuration
    /// </summary>
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly string _connectionString;

        public ConfigurationRepository()
        {
            _connectionString = $"Data Source={AppConstants.DatabasePath};Version=3;";
            InitializeDatabase();
        }

        public async Task<string> GetValueAsync(string key)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Value FROM Configuration WHERE Key = @key";
                command.Parameters.AddWithValue("@key", key);

                var result = await command.ExecuteScalarAsync();
                return result as string;
            }
        }

        public async Task SetValueAsync(string key, string value)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Check if key exists
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM Configuration WHERE Key = @key";
                checkCommand.Parameters.AddWithValue("@key", key);
                var exists = (long)await checkCommand.ExecuteScalarAsync() > 0;

                var command = connection.CreateCommand();
                if (exists)
                {
                    command.CommandText = @"
                        UPDATE Configuration SET Value = @value, UpdatedAt = @updatedAt
                        WHERE Key = @key";
                }
                else
                {
                    command.CommandText = @"
                        INSERT INTO Configuration (Key, Value, CreatedAt, UpdatedAt)
                        VALUES (@key, @value, @createdAt, @updatedAt)";
                    command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.Ticks);
                }

                command.Parameters.AddWithValue("@key", key);
                command.Parameters.AddWithValue("@value", value ?? "");
                command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.Ticks);

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> DeleteAsync(string key)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Configuration WHERE Key = @key";
                command.Parameters.AddWithValue("@key", key);
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }

        public async Task<Dictionary<string, string>> GetAllAsync()
        {
            var config = new Dictionary<string, string>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Key, Value FROM Configuration ORDER BY Key";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        config[reader["Key"].ToString()] = reader["Value"].ToString();
                    }
                }
            }
            return config;
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
                        CREATE TABLE IF NOT EXISTS Configuration (
                            Key TEXT PRIMARY KEY,
                            Value TEXT,
                            CreatedAt INTEGER,
                            UpdatedAt INTEGER
                        )";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Failed to initialize configuration table: {ex.Message}", ex);
            }
        }
    }
}
