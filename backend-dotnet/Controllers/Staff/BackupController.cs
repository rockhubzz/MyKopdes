using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/backup")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;

    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [HttpPost]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<object>> Create([FromQuery] string? outputFile = null)
    {
        var result = await _backupService.CreateBackupAsync(outputFile);
        return Ok(result);
    }

    [HttpGet("last")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<object>> GetLast()
    {
        var path = await _backupService.GetLastBackupPathAsync();
        return Ok(new { path });
    }

    [HttpPost("restore")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<bool>> Restore([FromQuery] string backupFile)
    {
        var success = await _backupService.RestoreBackupAsync(backupFile);
        return Ok(new { success });
    }
}
