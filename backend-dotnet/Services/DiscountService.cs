using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class DiscountService : IDiscountService
{
    private readonly KoperasiDbContext _context;

    public DiscountService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<Discount>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null, bool activeOnly = false, string? scope = null)
    {
        var query = _context.Discounts.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d => d.Name.Contains(search) || (d.Description != null && d.Description.Contains(search)));
        }
        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }
        if (!string.IsNullOrWhiteSpace(scope)) query = query.Where(d => d.Scope == scope);
        return await query
            .Include(d => d.Category)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? search = null, bool activeOnly = false, string? scope = null)
    {
        var query = _context.Discounts.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d => d.Name.Contains(search) || (d.Description != null && d.Description.Contains(search)));
        }
        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }
        if (!string.IsNullOrWhiteSpace(scope)) query = query.Where(d => d.Scope == scope);
        return await query.CountAsync();
    }

    public async Task<Discount?> GetByIdAsync(int id)
    {
        return await _context.Discounts
            .Include(d => d.Category)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Discount> CreateAsync(Discount discount)
    {
        discount.CreatedAt = DateTime.UtcNow;
        discount.UpdatedAt = DateTime.UtcNow;
        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync();
        return discount;
    }

    public async Task<Discount> UpdateAsync(int id, Discount discount)
    {
        var existing = await _context.Discounts.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException("Discount not found");

        existing.Name = discount.Name ?? existing.Name;
        existing.Description = discount.Description ?? existing.Description;
        existing.ItemId = discount.ItemId ?? existing.ItemId;
        existing.CategoryId = discount.CategoryId ?? existing.CategoryId;
        existing.MemberId = discount.MemberId ?? existing.MemberId;
        if (discount.DiscountRate != 0)
        {
            existing.DiscountRate = discount.DiscountRate;
        }
        existing.DiscountAmount = discount.DiscountAmount ?? existing.DiscountAmount;
        existing.Type = discount.Type ?? existing.Type;
        existing.StartDate = discount.StartDate ?? existing.StartDate;
        existing.EndDate = discount.EndDate ?? existing.EndDate;
        existing.IsActive = discount.IsActive;
        existing.Scope = discount.Scope ?? existing.Scope;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, bool force = false)
    {
        var discount = await _context.Discounts.FindAsync(id);
        if (discount == null) return false;

        if (force)
        {
            if (await _context.Transactions.AnyAsync(t => t.DiscountId == id))
                throw new InvalidOperationException("Cannot permanently delete a discount used in past transactions. Deactivate it instead.");
            _context.Discounts.Remove(discount);
        }
        else
        {
            // Plain DELETE from the UI = deactivate (soft), matching how
            // items/members/users behave so a deactivated discount can be
            // re-enabled with PUT { is_active: true }.
            discount.IsActive = false;
            discount.UpdatedAt = DateTime.UtcNow;
            _context.Discounts.Update(discount);
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetActivationAsync(int id, bool active)
    {
        var discount = await _context.Discounts.FindAsync(id);
        if (discount == null) return false;
        discount.IsActive = active;
        discount.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Discount>> GetActiveAsync(bool has_member = false)
    {
        var now = DateTime.UtcNow;
        var query = _context.Discounts
            .Include(d => d.Category)
            .Where(d => d.IsActive &&
                (d.StartDate == null || d.StartDate <= now) &&
                (d.EndDate == null || d.EndDate >= now));

        query = has_member
            ? query.Where(d => d.Scope == "member" || d.Scope == "general")
            : query.Where(d => d.Scope != "member");

        return await query.ToListAsync();
    }

    public async Task<List<Discount>> GetActiveDiscountsAsync(int? memberId = null, int? itemId = null, int? categoryId = null)
    {
        var now = DateTime.UtcNow;
        var query = _context.Discounts
            .Where(d => d.IsActive &&
                (d.StartDate == null || d.StartDate <= now) &&
                (d.EndDate == null || d.EndDate >= now));

        if (memberId.HasValue)
            query = query.Where(d => d.Scope == "member" || d.Scope == "general");

        if (itemId.HasValue)
            query = query.Where(d => d.ItemId == itemId.Value || d.ItemId == null);

        if (categoryId.HasValue)
            query = query.Where(d => d.CategoryId == categoryId.Value || d.CategoryId == null);

        return await query.ToListAsync();
    }

    public async Task<decimal> CalculateDiscountAsync(int? memberId, int? itemId, decimal subtotal)
    {
        var now = DateTime.UtcNow;
        var discounts = await _context.Discounts
            .Where(d => d.IsActive &&
                (d.StartDate == null || d.StartDate <= now) &&
                (d.EndDate == null || d.EndDate >= now))
            .ToListAsync();

        decimal totalDiscount = 0;

        foreach (var discount in discounts)
        {
            // Check scope match
            bool scopeMatch = discount.Scope == "general" ||
                (discount.Scope == "member" && memberId.HasValue) ||
                (discount.ItemId == itemId && discount.Scope == "item") ||
                (discount.Scope == "category" && discount.ItemId == null) ||
                discount.MemberId == null && discount.ItemId == null && discount.CategoryId == null;

            if (!scopeMatch) continue;

            if (discount.Type == "percentage")
            {
                totalDiscount += subtotal * (discount.DiscountRate / 100);
            }
            else if (discount.Type == "fixed")
            {
                totalDiscount += discount.DiscountAmount ?? 0;
            }
        }

        return Math.Min(totalDiscount, subtotal);
    }
}
