using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IRestockService
{
    Task<List<RestockingRecord>> GetAllAsync(int page = 1, int pageSize = 15);
    Task<RestockingRecord?> GetByIdAsync(int id);
    Task<RestockingRecord> CreateAsync(RestockingRecord record, int userId);
    Task<List<RestockingRecord>> GetByItemAsync(int itemId);
    Task<(List<RestockingRecord> Records, int Total)> GetPagedAsync(int page, int pageSize, int? itemId = null, int? userId = null);
    Task<decimal> GetTotalRestockCostAsync(DateTime? from = null, DateTime? to = null);
}
