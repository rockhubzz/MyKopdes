using MyKopdes.Models;

namespace MyKopdes.Services;

public interface ILoanProductService
{
    Task<List<LoanProduct>> GetAllAsync(int page = 1, int pageSize = 15);
    Task<LoanProduct?> GetByIdAsync(int id);
    Task<LoanProduct> CreateAsync(LoanProduct product);
    Task<LoanProduct> UpdateAsync(int id, LoanProduct product);
    Task<bool> DeleteAsync(int id);
    Task<List<LoanSetting>> GetSettingsAsync();
    Task<LoanSetting> UpdateSettingAsync(string key, string value);
    Task<decimal> GetSettingValueAsync(string key, decimal defaultValue = 0);
    Task<int> GetTotalCountAsync();
    Task<List<LoanProduct>> GetActiveAsync();
}
