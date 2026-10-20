using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class RestockService : IRestockService
{
    private readonly KoperasiDbContext _context;

    public RestockService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<RestockingRecord>> GetAllAsync(int page = 1, int pageSize = 15)
    {
        return await _context.RestockingRecords
            .Include(r => r.Item)
            .Include(r => r.Supplier)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<RestockingRecord?> GetByIdAsync(int id)
    {
        return await _context.RestockingRecords
            .Include(r => r.Item)
            .Include(r => r.Supplier)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<RestockingRecord> CreateAsync(RestockingRecord record, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        record.UserId = userId;
        record.RestockDate = record.RestockDate ?? DateTime.UtcNow;
        record.CreatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        _context.RestockingRecords.Add(record);
        await _context.SaveChangesAsync();

        // Update item stock and cost
        var item = await _context.Items.FindAsync(record.ItemId);
        if (item != null)
        {
            item.Stock += record.Quantity;
            // Update average cost: (old_cost * old_qty + new_cost * new_qty) / total_qty
            var oldTotal = (item.Cost ?? 0) * (item.Stock - record.Quantity ?? 0);
            var newTotal = oldTotal + ((record.CostPerUnit ?? 0) * record.Quantity);
            item.Cost = item.Stock > 0 ? newTotal / item.Stock : 0;
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return await GetByIdAsync(record.Id) ?? record;
    }

    public async Task<List<RestockingRecord>> GetByItemAsync(int itemId)
    {
        return await _context.RestockingRecords
            .Include(r => r.Supplier)
            .Where(r => r.ItemId == itemId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalRestockCostAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _context.RestockingRecords.AsQueryable();
        if (from.HasValue) query = query.Where(r => r.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(r => r.CreatedAt <= to.Value);
        return await query.SumAsync(r => r.TotalCost ?? 0);
    }
}
