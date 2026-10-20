using Microsoft.AspNetCore.Http;
using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IItemService
{
    Task<List<Item>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null, int? categoryId = null, bool lowStock = false, bool activeOnly = true);
    Task<Item?> GetByIdAsync(int id);
    Task<Item?> ScanAsync(string code);
    Task<Item> CreateAsync(Item item);
    Task<Item> UpdateAsync(int id, Item item);
    Task<Item?> ActivateAsync(int id);
    Task<bool> DeleteAsync(int id, bool force = false);
    Task<List<Item>> SearchAsync(string query);
    Task<List<Item>> GetLowStockAsync(int threshold);
    Task<int> GetTotalCountAsync(string? search = null, int? categoryId = null, bool lowStock = false, bool activeOnly = true);
}
