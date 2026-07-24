#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;

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
