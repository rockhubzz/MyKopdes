using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Middleware;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/backup")]
[Route("api/backups")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly IAuditLogService _auditLogService;

    public BackupController(IBackupService backupService, IAuditLogService auditLogService)
    {
        _backupService = backupService;
        _auditLogService = auditLogService;
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<object>> Create([FromQuery] string? outputFile = null)
    {
        var result = await _backupService.CreateBackupAsync(outputFile);
        if (result.TryGetValue("success", out var success) && success is true)
        {
            await _auditLogService.CreateAsync(new AuditLog
            {
                UserId = User.GetUserId(),
                UserType = "staff",
                Action = "backup_triggered",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
        }
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IReadOnlyList<BackupFileInfo>>> List()
    {
        return Ok(await _backupService.ListBackupFilesAsync());
    }

    [HttpGet("last")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<object>> GetLast()
    {
        var path = await _backupService.GetLastBackupPathAsync();
        return Ok(new { path });
    }

    [HttpPost("restore")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<bool>> Restore(
        [FromBody] BackupRestoreRequest? request = null,
        [FromQuery] string? backupFile = null)
    {
        var filename = request?.Filename ?? backupFile;
        if (string.IsNullOrWhiteSpace(filename))
            return BadRequest(new { message = "A backup filename is required" });

        var path = await _backupService.ResolveBackupPathAsync(filename);
        if (path == null) return NotFound(new { message = "Backup file not found" });
        var success = await _backupService.RestoreBackupAsync(path);
        if (success)
        {
            await _auditLogService.CreateAsync(new AuditLog
            {
                UserId = User.GetUserId(),
                UserType = "staff",
                Action = "backup_restored",
                Details = System.Text.Json.JsonSerializer.Serialize(new { filename }),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
        }
        return Ok(new { success });
    }

    [HttpGet("{filename}/download")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Download(string filename)
    {
        var path = await _backupService.ResolveBackupPathAsync(filename);
        if (path == null) return NotFound(new { message = "Backup file not found" });
        return PhysicalFile(path, "application/sql", Path.GetFileName(path));
    }
}

public class BackupRestoreRequest
{
    public string Filename { get; set; } = null!;
}
