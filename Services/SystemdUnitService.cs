// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Services
{
    /// <summary>
    /// Service for managing systemd unit files and service operations
    /// </summary>
    public class SystemdUnitService
    {
        public SystemdUnitService()
        {
            // Verify systemd is available
            VerifySystemdAvailable();
        }

        /// <summary>
        /// Create and enable a systemd unit for a service
        /// </summary>
        public async Task<bool> CreateUnitFileAsync(SystemdUnitConfig config, ManagedService service)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (service == null)
                throw new ArgumentNullException(nameof(service));

            config.Validate();

            try
            {
                var content = config.GenerateSystemdContent();
                var filePath = Path.Combine(AppConstants.SystemdUnitsDirectory, config.UnitName);

                if (!Directory.Exists(AppConstants.SystemdUnitsDirectory))
                    throw new SystemdOperationException($"Systemd directory not found: {AppConstants.SystemdUnitsDirectory}");

                await File.WriteAllTextAsync(filePath, content);

                // Reload systemd daemon
                await ExecuteSystemdCommandAsync("daemon-reload");

                return true;
            }
            catch (Exception ex)
            {
                throw new SystemdOperationException($"Failed to create unit file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete a systemd unit file
        /// </summary>
        public async Task<bool> DeleteUnitFileAsync(string unitName)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                throw new ArgumentException("Unit name is required", nameof(unitName));

            try
            {
                var filePath = Path.Combine(AppConstants.SystemdUnitsDirectory, unitName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    await ExecuteSystemdCommandAsync("daemon-reload");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new SystemdOperationException($"Failed to delete unit file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Start a service
        /// </summary>
        public async Task<bool> StartServiceAsync(string unitName)
        {
            return await ExecuteServiceCommandAsync("start", unitName);
        }

        /// <summary>
        /// Stop a service
        /// </summary>
        public async Task<bool> StopServiceAsync(string unitName)
        {
            return await ExecuteServiceCommandAsync("stop", unitName);
        }

        /// <summary>
        /// Restart a service
        /// </summary>
        public async Task<bool> RestartServiceAsync(string unitName)
        {
            return await ExecuteServiceCommandAsync("restart", unitName);
        }

        /// <summary>
        /// Reload service configuration
        /// </summary>
        public async Task<bool> ReloadServiceAsync(string unitName)
        {
            return await ExecuteServiceCommandAsync("reload", unitName);
        }

        /// <summary>
        /// Enable service to start on boot
        /// </summary>
        public async Task<bool> EnableServiceAsync(string unitName)
        {
            return await ExecuteServiceCommandAsync("enable", unitName);
        }

        /// <summary>
        /// Disable service from starting on boot
        /// </summary>
        public async Task<bool> DisableServiceAsync(string unitName)
        {
            return await ExecuteServiceCommandAsync("disable", unitName);
        }

        /// <summary>
        /// Get service status
        /// </summary>
        public async Task<ServiceStatus> GetServiceStatusAsync(string unitName)
        {
            try
            {
                var output = await ExecuteCommandAsync("systemctl", $"is-active {unitName}", captureOutput: true);

                return output.Trim() switch
                {
                    "active" => ServiceStatus.Running,
                    "inactive" => ServiceStatus.Stopped,
                    "activating" => ServiceStatus.Restarting,
                    "failed" => ServiceStatus.Failed,
                    _ => ServiceStatus.Unknown
                };
            }
            catch
            {
                return ServiceStatus.Unknown;
            }
        }

        /// <summary>
        /// Check if unit file exists
        /// </summary>
        public bool UnitFileExists(string unitName)
        {
            var filePath = Path.Combine(AppConstants.SystemdUnitsDirectory, unitName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// Get unit file content
        /// </summary>
        public async Task<string> GetUnitFileContentAsync(string unitName)
        {
            var filePath = Path.Combine(AppConstants.SystemdUnitsDirectory, unitName);

            if (!File.Exists(filePath))
                throw new SystemdOperationException($"Unit file not found: {unitName}");

            return await File.ReadAllTextAsync(filePath);
        }

        /// <summary>
        /// List all service units
        /// </summary>
        public async Task<List<string>> ListUnitsAsync()
        {
            try
            {
                var units = new List<string>();
                var directory = new DirectoryInfo(AppConstants.SystemdUnitsDirectory);

                if (!directory.Exists)
                    return units;

                var files = directory.GetFiles("*.service");
                foreach (var file in files)
                {
                    units.Add(file.Name);
                }

                return units;
            }
            catch (Exception ex)
            {
                throw new SystemdOperationException($"Failed to list units: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get unit status details
        /// </summary>
        public async Task<SystemdUnitStatus> GetDetailedStatusAsync(string unitName)
        {
            try
            {
                var statusOutput = await ExecuteCommandAsync("systemctl", $"show {unitName}", captureOutput: true);
                var status = new SystemdUnitStatus { UnitName = unitName };

                var lines = statusOutput.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("ActiveState="))
                        status.ActiveState = line.Substring("ActiveState=".Length);
                    else if (line.StartsWith("Type="))
                        status.Type = line.Substring("Type=".Length);
                    else if (line.StartsWith("ExecMainPID="))
                        int.TryParse(line.Substring("ExecMainPID=".Length), out status.MainPid);
                    else if (line.StartsWith("TimeoutStartUSec="))
                        ulong.TryParse(line.Substring("TimeoutStartUSec=".Length), out status.TimeoutStartUsec);
                }

                return status;
            }
            catch (Exception ex)
            {
                throw new SystemdOperationException($"Failed to get status: {ex.Message}", ex);
            }
        }

        private async Task<bool> ExecuteServiceCommandAsync(string command, string unitName)
        {
            try
            {
                await ExecuteCommandAsync("systemctl", $"{command} {unitName}");
                return true;
            }
            catch (Exception ex)
            {
                throw new SystemdOperationException($"Failed to {command} service: {ex.Message}", ex);
            }
        }

        private async Task<bool> ExecuteSystemdCommandAsync(string command)
        {
            try
            {
                await ExecuteCommandAsync("systemctl", command);
                return true;
            }
            catch (Exception ex)
            {
                throw new SystemdOperationException($"Systemd command failed: {ex.Message}", ex);
            }
        }

        private async Task<string> ExecuteCommandAsync(string program, string arguments, bool captureOutput = false)
        {
            var psi = new ProcessStartInfo
            {
                FileName = program,
                Arguments = arguments,
                RedirectStandardOutput = captureOutput,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                var outputTask = captureOutput ? process.StandardOutput.ReadToEndAsync() : Task.FromResult("");
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(outputTask, errorTask);

                if (!process.WaitForExit(AppConstants.SystemdCommandTimeoutSeconds * 1000))
                {
                    process.Kill();
                    throw new TimeoutException($"Command '{program} {arguments}' timed out");
                }

                if (process.ExitCode != 0)
                    throw new SystemdOperationException($"Command failed with exit code {process.ExitCode}: {errorTask.Result}");

                return await outputTask;
            }
        }

        private void VerifySystemdAvailable()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode != 0)
                        throw new SystemdOperationException("Systemd is not available");
                }
            }
            catch (Exception ex)
            {
                throw new SystemdOperationException("Failed to verify systemd availability", ex);
            }
        }
    }

    public class SystemdUnitStatus
    {
        public string UnitName { get; set; }
        public string ActiveState { get; set; }
        public string Type { get; set; }
        public int MainPid { get; set; }
        public ulong TimeoutStartUsec { get; set; }
    }
}
