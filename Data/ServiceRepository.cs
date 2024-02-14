#nullable enable
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
    /// SQLite-based repository for managing services
    /// </summary>
    public sealed class ServiceRepository : IServiceRepository
    {
        private readonly string _connectionString;

        public ServiceRepository()
        {
            _connectionString = $"Data Source={AppConstants.DatabasePath};Version=3;";
            InitializeDatabase();
        }

        public async Task<ManagedService> GetByIdAsync(string id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Services WHERE Id = @id";
                command.Parameters.AddWithValue("@id", id);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        return MapReaderToService(reader);
                }
            }
            return null;
        }

        public async Task<ManagedService> GetByNameAsync(string name)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Services WHERE Name = @name";
                command.Parameters.AddWithValue("@name", name);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        return MapReaderToService(reader);
                }
            }
            return null;
        }

        public async Task<List<ManagedService>> GetAllAsync()
        {
            var services = new List<ManagedService>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Services ORDER BY Name";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        services.Add(MapReaderToService(reader));
                }
            }
            return services;
        }

        public async Task<List<ManagedService>> GetByTypeAsync(ServiceType type)
        {
            var services = new List<ManagedService>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Services WHERE Type = @type ORDER BY Name";
                command.Parameters.AddWithValue("@type", (int)type);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        services.Add(MapReaderToService(reader));
                }
            }
            return services;
        }

        public async Task<List<ManagedService>> GetEnabledServicesAsync()
        {
            var services = new List<ManagedService>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Services WHERE IsEnabled = 1 ORDER BY Name";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        services.Add(MapReaderToService(reader));
                }
            }
            return services;
        }

        public async Task<string> AddAsync(ManagedService service)
        {
            service.Validate();
            if (string.IsNullOrEmpty(service.Id))
                service.Id = Guid.NewGuid().ToString();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Services (Id, Name, Description, Type, ExecutablePath, WorkingDirectory,
                        Arguments, Status, Port, HostBinding, AutoStart, Priority, EnvironmentVariables,
                        CreatedAt, UpdatedAt, SystemdUnitName, IsEnabled)
                    VALUES (@id, @name, @description, @type, @execPath, @workDir,
                        @args, @status, @port, @hostBinding, @autoStart, @priority, @envVars,
                        @createdAt, @updatedAt, @systemdUnit, @isEnabled)";

                command.Parameters.AddWithValue("@id", service.Id);
                command.Parameters.AddWithValue("@name", service.Name);
                command.Parameters.AddWithValue("@description", service.Description ?? "");
                command.Parameters.AddWithValue("@type", (int)service.Type);
                command.Parameters.AddWithValue("@execPath", service.ExecutablePath);
                command.Parameters.AddWithValue("@workDir", service.WorkingDirectory);
                command.Parameters.AddWithValue("@args", service.Arguments ?? "");
                command.Parameters.AddWithValue("@status", (int)service.Status);
                command.Parameters.AddWithValue("@port", service.Port);
                command.Parameters.AddWithValue("@hostBinding", service.HostBinding);
                command.Parameters.AddWithValue("@autoStart", service.AutoStart ? 1 : 0);
                command.Parameters.AddWithValue("@priority", service.Priority);
                command.Parameters.AddWithValue("@envVars", service.EnvironmentVariables ?? "");
                command.Parameters.AddWithValue("@createdAt", service.CreatedAt.Ticks);
                command.Parameters.AddWithValue("@updatedAt", service.UpdatedAt.Ticks);
                command.Parameters.AddWithValue("@systemdUnit", service.SystemdUnitName ?? "");
                command.Parameters.AddWithValue("@isEnabled", service.IsEnabled ? 1 : 0);

                await command.ExecuteNonQueryAsync();
            }
            return service.Id;
        }

        public async Task<bool> UpdateAsync(ManagedService service)
        {
            service.Validate();
            service.UpdatedAt = DateTime.UtcNow;

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Services SET Name = @name, Description = @description, Type = @type,
                        ExecutablePath = @execPath, WorkingDirectory = @workDir, Arguments = @args,
                        Status = @status, Port = @port, HostBinding = @hostBinding, AutoStart = @autoStart,
                        Priority = @priority, EnvironmentVariables = @envVars, UpdatedAt = @updatedAt,
                        SystemdUnitName = @systemdUnit, IsEnabled = @isEnabled
                    WHERE Id = @id";

                command.Parameters.AddWithValue("@id", service.Id);
                command.Parameters.AddWithValue("@name", service.Name);
                command.Parameters.AddWithValue("@description", service.Description ?? "");
                command.Parameters.AddWithValue("@type", (int)service.Type);
                command.Parameters.AddWithValue("@execPath", service.ExecutablePath);
                command.Parameters.AddWithValue("@workDir", service.WorkingDirectory);
                command.Parameters.AddWithValue("@args", service.Arguments ?? "");
                command.Parameters.AddWithValue("@status", (int)service.Status);
                command.Parameters.AddWithValue("@port", service.Port);
                command.Parameters.AddWithValue("@hostBinding", service.HostBinding);
                command.Parameters.AddWithValue("@autoStart", service.AutoStart ? 1 : 0);
                command.Parameters.AddWithValue("@priority", service.Priority);
                command.Parameters.AddWithValue("@envVars", service.EnvironmentVariables ?? "");
                command.Parameters.AddWithValue("@updatedAt", service.UpdatedAt.Ticks);
                command.Parameters.AddWithValue("@systemdUnit", service.SystemdUnitName ?? "");
                command.Parameters.AddWithValue("@isEnabled", service.IsEnabled ? 1 : 0);

                return await command.ExecuteNonQueryAsync() > 0;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Services WHERE Id = @id";
                command.Parameters.AddWithValue("@id", id);
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }

        public async Task<bool> ExistsAsync(string id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Services WHERE Id = @id";
                command.Parameters.AddWithValue("@id", id);
                var result = await command.ExecuteScalarAsync();
                return result is not null && (long)result > 0;
            }
        }

        public async Task<int> GetCountAsync()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Services";
                var result = await command.ExecuteScalarAsync();
                return result is not null ? (int)(long)result : 0;
            }
        }

        public async Task<List<ManagedService>> SearchAsync(string query)
        {
            var services = new List<ManagedService>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Services WHERE Name LIKE @query OR Description LIKE @query ORDER BY Name";
                command.Parameters.AddWithValue("@query", $"%{query}%");

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        services.Add(MapReaderToService(reader));
                }
            }
            return services;
        }

        private ManagedService MapReaderToService(System.Data.Common.DbDataReader reader)
        {
            return new ManagedService
            {
                Id = reader["Id"].ToString(),
                Name = reader["Name"].ToString(),
                Description = reader["Description"].ToString(),
                Type = (ServiceType)(long)reader["Type"],
                ExecutablePath = reader["ExecutablePath"].ToString(),
                WorkingDirectory = reader["WorkingDirectory"].ToString(),
                Arguments = reader["Arguments"].ToString(),
                Status = (ServiceStatus)(long)reader["Status"],
                Port = (int)(long)reader["Port"],
                HostBinding = reader["HostBinding"].ToString(),
                AutoStart = (long)reader["AutoStart"] > 0,
                Priority = (int)(long)reader["Priority"],
                EnvironmentVariables = reader["EnvironmentVariables"].ToString(),
                CreatedAt = new DateTime((long)reader["CreatedAt"]),
                UpdatedAt = new DateTime((long)reader["UpdatedAt"]),
                SystemdUnitName = reader["SystemdUnitName"].ToString(),
                IsEnabled = (long)reader["IsEnabled"] > 0
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
                        CREATE TABLE IF NOT EXISTS Services (
                            Id TEXT PRIMARY KEY,
                            Name TEXT NOT NULL UNIQUE,
                            Description TEXT,
                            Type INTEGER,
                            ExecutablePath TEXT NOT NULL,
                            WorkingDirectory TEXT NOT NULL,
                            Arguments TEXT,
                            Status INTEGER,
                            Port INTEGER,
                            HostBinding TEXT,
                            AutoStart INTEGER,
                            Priority INTEGER,
                            EnvironmentVariables TEXT,
                            CreatedAt INTEGER,
                            UpdatedAt INTEGER,
                            SystemdUnitName TEXT,
                            IsEnabled INTEGER
                        )";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Failed to initialize database: {ex.Message}", ex);
            }
        }
    }
}
