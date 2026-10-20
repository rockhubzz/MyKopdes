using MyKopdes.Models;

namespace MyKopdes.Services;

public interface ILoanProductService
{
    Task<List<LoanProduct>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null, bool activeOnly = false);
    Task<LoanProduct?> GetByIdAsync(int id);
    Task<LoanProduct> CreateAsync(LoanProduct product);
    Task<LoanProduct> UpdateAsync(int id, LoanProduct product);
    Task<bool> DeleteAsync(int id, bool force = false);
    Task<bool> SetActivationAsync(int id, bool active);
    Task<List<LoanSetting>> GetSettingsAsync();
    Task<LoanSetting> UpdateSettingAsync(string key, string value);
    Task<decimal> GetSettingValueAsync(string key, decimal defaultValue = 0);
    Task<int> GetTotalCountAsync(string? search = null, bool activeOnly = false);
    Task<List<LoanProduct>> GetActiveAsync();
}
