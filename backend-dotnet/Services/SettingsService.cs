using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyKopdes.Data;
using MyKopdes.Models;
using Microsoft.Extensions.Configuration;

namespace MyKopdes.Services;

public class SettingsService : ISettingsService
{
    private readonly KoperasiDbContext _context;
    private readonly IConfiguration _config;
    private readonly IMemoryCache? _cache;

    public SettingsService(KoperasiDbContext context, IConfiguration config, IMemoryCache? cache = null)
    {
        _context = context;
        _config = config;
        _cache = cache;
    }

    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        var settings = await _context.Settings.ToDictionaryAsync(s => s.Key, s => s.Value ?? "");
        return settings;
    }

    public async Task<Dictionary<string, string>> GetPublicAsync()
    {
        return await _context.Settings
            .AsNoTracking()
            .Where(s => s.IsPublic)
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? "");
    }

    public async Task<Setting?> GetByKeyAsync(string key)
    {
        return await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
    }

    public async Task<Setting> UpdateOrCreateAsync(string key, string value, string? group = null)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new Setting
            {
                Key = key,
                Value = value,
                Group = group,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.Group = group;
            setting.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        // Invalidate the checkout tax-rate cache when the tax_rate setting
        // changes so the next preview/checkout uses the new value immediately.
        if (_cache != null && setting.Key == "tax_rate")
        {
            _cache.Remove("tax_rate");
        }

        return setting;
    }

    public async Task<Dictionary<string, string>> GetDefaultsAsync()
    {
        var defaults = new Dictionary<string, string>
        {
            ["tax_rate"] = _config.GetValue<string>("Koperasi:DefaultTaxRate") ?? "0",
            ["shu_rate"] = _config.GetValue<string>("Koperasi:DefaultShuRate") ?? "0.02",
            ["low_stock_threshold"] = _config.GetValue<string>("Koperasi:LowStockThreshold") ?? "10",
            ["backup_schedule_cron"] = _config.GetValue<string>("Koperasi:BackupScheduleCron") ?? "0 2 * * *",
            ["api_base_url"] = _config.GetValue<string>("Koperasi:ApiBaseUrl") ?? "http://localhost"
        };

        // Override with any DB values
        var dbSettings = await GetAllAsync();
        foreach (var kvp in dbSettings)
        {
            defaults[kvp.Key] = kvp.Value;
        }

        return defaults;
    }
}
