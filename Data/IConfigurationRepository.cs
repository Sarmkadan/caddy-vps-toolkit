#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CaddyVpsToolkit.Core;
using CaddyVpsToolkit.Domain.Models;

namespace CaddyVpsToolkit.Data
{
    /// <summary>
    /// Repository interface for application configuration
    /// </summary>
    public interface IConfigurationRepository
    {
        Task<string> GetValueAsync(string key);
        Task SetValueAsync(string key, string value);
        Task<bool> DeleteAsync(string key);
        Task<Dictionary<string, string>> GetAllAsync();
    }
}