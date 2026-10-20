using MyKopdes.Models;

namespace MyKopdes.Services;

public interface ISettingsService
{
    Task<Dictionary<string, string>> GetAllAsync();
    Task<Setting?> GetByKeyAsync(string key);
    Task<Setting> UpdateOrCreateAsync(string key, string value, string? group = null);
    Task<Dictionary<string, string>> GetDefaultsAsync();
}
