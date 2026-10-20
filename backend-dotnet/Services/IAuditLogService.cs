using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IAuditLogService
{
    Task<List<AuditLog>> GetAllAsync(int page = 1, int pageSize = 50);
    Task<int> GetTotalCountAsync();
    Task<AuditLog?> GetByIdAsync(int id);
    Task<AuditLog> CreateAsync(AuditLog log);
    Task<List<AuditLog>> GetByEntityTypeAsync(string entityType, int? entityId = null);
}
