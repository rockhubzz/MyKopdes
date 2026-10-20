using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class ItemService : IItemService
{
    private readonly KoperasiDbContext _context;

    public ItemService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<Item>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null, int? categoryId = null, bool lowStock = false, bool activeOnly = true)
    {
        var query = _context.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(i => i.Name.Contains(search) || i.Code.Contains(search) || (i.Barcode != null && i.Barcode.Contains(search)));
        }
        if (categoryId.HasValue)
        {
            query = query.Where(i => i.CategoryId == categoryId.Value);
        }
        if (lowStock)
        {
            query = query.Where(i => (i.Stock ?? 0) <= (i.MinStock ?? 0));
        }
        if (activeOnly)
        {
            query = query.Where(i => i.IsActive);
        }

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Item?> GetByIdAsync(int id)
    {
        return await _context.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Item?> ScanAsync(string code)
    {
        return await _context.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.IsActive && (i.Barcode == code || i.Code == code));
    }

    public async Task<Item> CreateAsync(Item item)
    {
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<Item> UpdateAsync(int id, Item item)
    {
        var existing = await _context.Items.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException("Item not found");

        existing.Name = item.Name;
        existing.Code = item.Code;
        existing.Barcode = item.Barcode;
        existing.CategoryId = item.CategoryId;
        existing.SupplierId = item.SupplierId;
        existing.Price = item.Price;
        existing.Cost = item.Cost;
        existing.Stock = item.Stock;
        existing.MinStock = item.MinStock;
        existing.Unit = item.Unit;
        existing.ExpiryDate = item.ExpiryDate;
        existing.IsActive = item.IsActive;
        existing.Image = item.Image;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<Item?> ActivateAsync(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null) return null;
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(int id, bool force = false)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null) return false;
        if (force)
        {
            _context.Items.Remove(item);
        }
        else
        {
            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Item>> SearchAsync(string query)
    {
        return await _context.Items
            .AsNoTracking()
            .Where(i => i.Name.Contains(query) || i.Code.Contains(query))
            .ToListAsync();
    }

    public async Task<List<Item>> GetLowStockAsync(int threshold)
    {
        return await _context.Items
            .AsNoTracking()
            .Where(i => (i.Stock ?? 0) <= threshold)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? search = null, int? categoryId = null, bool lowStock = false, bool activeOnly = true)
    {
        var query = _context.Items.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(i => i.Name.Contains(search) || i.Code.Contains(search) || (i.Barcode != null && i.Barcode.Contains(search)));
        }
        if (categoryId.HasValue)
        {
            query = query.Where(i => i.CategoryId == categoryId.Value);
        }
        if (lowStock)
        {
            query = query.Where(i => (i.Stock ?? 0) <= (i.MinStock ?? 0));
        }
        if (activeOnly)
        {
            query = query.Where(i => i.IsActive);
        }
        return await query.CountAsync();
    }
}
