using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IDiscountService
{
    Task<List<Discount>> GetAllAsync(int page = 1, int pageSize = 15);
    Task<int> GetTotalCountAsync();
    Task<Discount?> GetByIdAsync(int id);
    Task<Discount> CreateAsync(Discount discount);
    Task<Discount> UpdateAsync(int id, Discount discount);
    Task<bool> DeleteAsync(int id, bool force = false);
    Task<decimal> CalculateDiscountAsync(int? memberId, int? itemId, decimal subtotal);
    Task<List<Discount>> GetActiveDiscountsAsync(int? memberId = null, int? itemId = null, int? categoryId = null);
    Task<List<Discount>> GetActiveAsync(bool has_member = false);
}
