using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/audit-logs")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,owner")]
    public async Task<ActionResult<PaginatedResponse<AuditLog>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 50)
    {
        var logs = await _auditLogService.GetAllAsync(page, pageSize);
        var total = await _auditLogService.GetTotalCountAsync();
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return Ok(new PaginatedResponse<AuditLog>
        {
            Data = logs,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,owner")]
    public async Task<ActionResult<AuditLog>> GetById(int id)
    {
        var log = await _auditLogService.GetByIdAsync(id);
        if (log == null) return NotFound(new { message = "Audit log not found" });
        return Ok(log);
    }

    [HttpGet("entity/{entityType}")]
    [Authorize(Roles = "admin,owner")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetByEntity(string entityType, [FromQuery] int? entityId = null)
    {
        var logs = await _auditLogService.GetByEntityTypeAsync(entityType, entityId);
        return Ok(logs);
    }
}
