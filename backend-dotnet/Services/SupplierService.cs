using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class SupplierService : ISupplierService
{
    private readonly KoperasiDbContext _context;

    public SupplierService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<Supplier>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null)
    {
        var query = _context.Suppliers.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s => s.Name.Contains(search) ||
                (s.ContactPerson != null && s.ContactPerson.Contains(search)) ||
                (s.Phone != null && s.Phone.Contains(search)));
        }
        return await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        return await _context.Suppliers
            .Include(s => s.RestockingRecords!.OrderByDescending(r => r.CreatedAt).Take(10))
            .ThenInclude(r => r.Item)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        supplier.CreatedAt = DateTime.UtcNow;
        supplier.UpdatedAt = DateTime.UtcNow;
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task<Supplier> UpdateAsync(int id, Supplier supplier)
    {
        var existing = await _context.Suppliers.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException("Supplier not found");

        existing.Name = supplier.Name;
        existing.Phone = supplier.Phone;
        existing.Address = supplier.Address;
        existing.Email = supplier.Email;
        existing.ContactPerson = supplier.ContactPerson;
        existing.Notes = supplier.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return false;
        if (await _context.RestockingRecords.AnyAsync(r => r.SupplierId == id))
            throw new InvalidOperationException("Cannot delete a supplier with restocking history.");
        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Supplier>> SearchAsync(string query)
    {
        return await _context.Suppliers
            .Where(s => s.Name.Contains(query) ||
                (s.ContactPerson != null && s.ContactPerson.Contains(query)) ||
                (s.Phone != null && s.Phone.Contains(query)))
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? search = null)
    {
        var query = _context.Suppliers.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s => s.Name.Contains(search) || (s.Phone != null && s.Phone.Contains(search)));
        }
        return await query.CountAsync();
    }
}
