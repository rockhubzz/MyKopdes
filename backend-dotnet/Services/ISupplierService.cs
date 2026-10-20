using MyKopdes.Models;

namespace MyKopdes.Services;

public interface ISupplierService
{
    Task<List<Supplier>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null);
    Task<Supplier?> GetByIdAsync(int id);
    Task<Supplier> CreateAsync(Supplier supplier);
    Task<Supplier> UpdateAsync(int id, Supplier supplier);
    Task<bool> DeleteAsync(int id);
    Task<List<Supplier>> SearchAsync(string query);
    Task<int> GetTotalCountAsync(string? search = null);
}
