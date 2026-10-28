using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IDiscountService
{
    Task<List<Discount>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null, bool activeOnly = false, string? scope = null);
    Task<int> GetTotalCountAsync(string? search = null, bool activeOnly = false, string? scope = null);
    Task<Discount?> GetByIdAsync(int id);
    Task<Discount> CreateAsync(Discount discount);
    Task<Discount> UpdateAsync(int id, Discount discount);
    Task<bool> DeleteAsync(int id, bool force = false);
    Task<bool> SetActivationAsync(int id, bool active);
    Task<decimal> CalculateDiscountAsync(int? memberId, int? itemId, decimal subtotal);
    Task<List<Discount>> GetActiveDiscountsAsync(int? memberId = null, int? itemId = null, int? categoryId = null);
    Task<List<Discount>> GetActiveAsync(bool has_member = false);
}
