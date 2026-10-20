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

        // Atomically bump stock and recompute the weighted average cost in a
        // single statement (cost_price is computed against the pre-update
        // stock: MySQL applies SET clauses left-to-right). Reading the item
        // into memory and writing absolute values back loses updates when
        // two restocks land concurrently.
        await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE items SET
                cost_price = CASE WHEN COALESCE(current_stock, 0) + {1} > 0
                    THEN ROUND(((COALESCE(cost_price, 0) * COALESCE(current_stock, 0)) + ({2} * {1})) / (COALESCE(current_stock, 0) + {1}), 2)
                    ELSE COALESCE(cost_price, 0) END,
                current_stock = COALESCE(current_stock, 0) + {1}
              WHERE id = {0}",
            record.ItemId, record.Quantity, record.CostPerUnit ?? 0);

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
