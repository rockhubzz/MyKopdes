using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class ItemCategoryService : IItemCategoryService
{
    private readonly KoperasiDbContext _context;

    public ItemCategoryService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<ItemCategory>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null)
    {
        var query = _context.ItemCategories.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Name.Contains(search));
        }
        return await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<ItemCategory?> GetByIdAsync(int id)
    {
        return await _context.ItemCategories.FindAsync(id);
    }

    public async Task<ItemCategory> CreateAsync(ItemCategory category)
    {
        category.CreatedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;
        _context.ItemCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<ItemCategory> UpdateAsync(int id, ItemCategory category)
    {
        var existing = await _context.ItemCategories.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException("Item category not found");

        existing.Name = category.Name;
        existing.Description = category.Description;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.ItemCategories.FindAsync(id);
        if (category == null) return false;
        _context.ItemCategories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetTotalCountAsync(string? search = null)
    {
        var query = _context.ItemCategories.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Name.Contains(search));
        }
        return await query.CountAsync();
    }
}
