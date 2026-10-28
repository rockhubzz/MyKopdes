using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IAuditLogService
{
    Task<List<AuditLog>> GetAllAsync(int page = 1, int pageSize = 50, string? action = null,
        int? actorId = null, DateTime? from = null, DateTime? to = null);
    Task<int> GetTotalCountAsync(string? action = null, int? actorId = null, DateTime? from = null, DateTime? to = null);
    Task<AuditLog?> GetByIdAsync(int id);
    Task<AuditLog> CreateAsync(AuditLog log);
    Task<List<AuditLog>> GetByEntityTypeAsync(string entityType, int? entityId = null);
}
