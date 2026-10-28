using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class AuditLogService : IAuditLogService
{
    private readonly KoperasiDbContext _context;

    public AuditLogService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLog>> GetAllAsync(int page = 1, int pageSize = 50, string? action = null,
        int? actorId = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action != null && a.Action.Contains(action));
        if (actorId.HasValue) query = query.Where(a => a.UserId == actorId.Value);
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value.Date);
        if (to.HasValue) query = query.Where(a => a.CreatedAt < to.Value.Date.AddDays(1));

        return await query
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? action = null, int? actorId = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action != null && a.Action.Contains(action));
        if (actorId.HasValue) query = query.Where(a => a.UserId == actorId.Value);
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value.Date);
        if (to.HasValue) query = query.Where(a => a.CreatedAt < to.Value.Date.AddDays(1));
        return await query.CountAsync();
    }

    public async Task<AuditLog?> GetByIdAsync(int id)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<AuditLog> CreateAsync(AuditLog log)
    {
        log.CreatedAt = DateTime.UtcNow;
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<List<AuditLog>> GetByEntityTypeAsync(string entityType, int? entityId = null)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType);

        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
    }
}
