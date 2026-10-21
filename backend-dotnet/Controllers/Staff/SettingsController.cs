using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<Dictionary<string, string>>> Get()
    {
        var settings = await _settingsService.GetAllAsync();
        return Ok(settings);
    }

    [HttpGet("defaults")]
    public async Task<ActionResult<Dictionary<string, string>>> GetDefaults()
    {
        var defaults = await _settingsService.GetDefaultsAsync();
        return Ok(defaults);
    }

    [HttpPut("{key}")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<Setting>> Update(string key, [FromBody] SettingUpdateRequest request)
    {
        var setting = await _settingsService.UpdateOrCreateAsync(key, request.Value, request.Group);
        return Ok(setting);
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<Setting?>> GetByKey(string key)
    {
        var setting = await _settingsService.GetByKeyAsync(key);
        return Ok(setting);
    }
}

public class SettingUpdateRequest
{
    public string Value { get; set; } = null!;
    public string? Group { get; set; }
}
