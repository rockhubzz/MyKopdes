using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IItemCategoryService
{
    Task<List<ItemCategory>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null);
    Task<ItemCategory?> GetByIdAsync(int id);
    Task<ItemCategory> CreateAsync(ItemCategory category);
    Task<ItemCategory> UpdateAsync(int id, ItemCategory category);
    Task<bool> DeleteAsync(int id);
    Task<int> GetTotalCountAsync(string? search = null);
}
