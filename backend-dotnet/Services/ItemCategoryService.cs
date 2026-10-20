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
        var query = _context.ItemCategories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Name.Contains(search));
        }
        var categories = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Item counts for the whole page in ONE grouped query (the Laravel
        // backend used withCount('items') — all items, including inactive).
        var counts = await _context.Items
            .Where(i => i.CategoryId != null)
            .GroupBy(i => i.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        foreach (var category in categories)
        {
            category.ItemsCount = counts.GetValueOrDefault(category.Id, 0);
        }

        return categories;
    }

    public async Task<ItemCategory?> GetByIdAsync(int id)
    {
        var category = await _context.ItemCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return null;

        category.ItemsCount = await _context.Items.CountAsync(i => i.CategoryId == id);
        return category;
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

        // Same guard the Laravel backend enforced: refuse to delete a
        // category that still has items assigned to it.
        var hasItems = await _context.Items.AnyAsync(i => i.CategoryId == id);
        if (hasItems) throw new InvalidOperationException(
            "Cannot delete a category that still has items. Reassign or remove those items first.");

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
