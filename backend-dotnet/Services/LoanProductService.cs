using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class LoanProductService : ILoanProductService
{
    private readonly KoperasiDbContext _context;

    public LoanProductService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<LoanProduct>> GetAllAsync(int page = 1, int pageSize = 15)
    {
        return await _context.LoanProducts
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<LoanProduct?> GetByIdAsync(int id)
    {
        return await _context.LoanProducts.FindAsync(id);
    }

    public async Task<LoanProduct> CreateAsync(LoanProduct product)
    {
        product.InterestRate ??= 0;
        product.MinTermMonths ??= 1;
        product.MaxTermMonths ??= 12;
        product.MinAmount ??= 0;
        product.MaxAmount ??= 0;
        product.ProcessingFeeRate ??= 0;
        product.IsActive ??= true;
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        _context.LoanProducts.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<LoanProduct> UpdateAsync(int id, LoanProduct product)
    {
        var existing = await _context.LoanProducts.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException("Loan product not found");

        existing.Name = product.Name ?? existing.Name;
        existing.Description = product.Description ?? existing.Description;
        existing.InterestRate = product.InterestRate ?? existing.InterestRate;
        existing.MinTermMonths = product.MinTermMonths ?? existing.MinTermMonths;
        existing.MaxTermMonths = product.MaxTermMonths ?? existing.MaxTermMonths;
        existing.MinAmount = product.MinAmount ?? existing.MinAmount;
        existing.MaxAmount = product.MaxAmount ?? existing.MaxAmount;
        existing.ProcessingFeeRate = product.ProcessingFeeRate ?? existing.ProcessingFeeRate;
        existing.IsActive = product.IsActive ?? existing.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.LoanProducts.FindAsync(id);
        if (product == null) return false;
        _context.LoanProducts.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<LoanSetting>> GetSettingsAsync()
    {
        return await _context.LoanSettings.ToListAsync();
    }

    public async Task<LoanSetting> UpdateSettingAsync(string key, string value)
    {
        var setting = await _context.LoanSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new LoanSetting
            {
                Key = key,
                Value = value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.LoanSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
        return setting;
    }

    public async Task<decimal> GetSettingValueAsync(string key, decimal defaultValue = 0)
    {
        var setting = await _context.LoanSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting?.Value == null) return defaultValue;
        return decimal.TryParse(setting.Value, out var val) ? val : defaultValue;
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.LoanProducts.CountAsync();
    }

    public async Task<List<LoanProduct>> GetActiveAsync()
    {
        return await _context.LoanProducts
            .Where(p => p.IsActive == true)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
