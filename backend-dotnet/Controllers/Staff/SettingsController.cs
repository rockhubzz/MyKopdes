using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/settings")]
[Route("api/admin/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly KoperasiDbContext _context;

    public SettingsController(ISettingsService settingsService, KoperasiDbContext context)
    {
        _settingsService = settingsService;
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<object>> Get()
    {
        if (Request.Path.StartsWithSegments("/api/admin/settings"))
        {
            var grouped = await _context.Settings
                .AsNoTracking()
                .ToListAsync();

            static Dictionary<string, string> Group(IEnumerable<Setting> values) =>
                values.ToDictionary(s => s.Key, s => s.Value ?? "");

            return Ok(new
            {
                infrastructure = Group(grouped.Where(s => s.Group == "infrastructure")),
                business_rules = Group(grouped.Where(s => s.Group == "business_rules")),
                backup = Group(grouped.Where(s => s.Group == "backup"))
            });
        }

        var settings = await _settingsService.GetAllAsync();
        return Ok(settings);
    }

    [HttpPut]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<object>> UpdateGroup([FromBody] SettingsGroupUpdateRequest request)
    {
        if (request.Values == null || request.Values.Count == 0)
            return BadRequest(new { message = "At least one setting value is required" });

        var allowedGroups = new[] { "infrastructure", "business_rules", "backup" };
        if (!allowedGroups.Contains(request.Group, StringComparer.Ordinal))
            return BadRequest(new { message = "Unknown settings group" });

        foreach (var pair in request.Values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key)) continue;
            await _settingsService.UpdateOrCreateAsync(pair.Key, pair.Value ?? "", request.Group);
        }

        return await Get();
    }

    [HttpGet("defaults")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Dictionary<string, string>>> GetDefaults()
    {
        var defaults = await _settingsService.GetDefaultsAsync();
        return Ok(defaults);
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<Dictionary<string, string>>> GetPublic()
    {
        return Ok(await _settingsService.GetPublicAsync());
    }

    [HttpPut("{key}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Setting>> Update(string key, [FromBody] SettingUpdateRequest request)
    {
        var setting = await _settingsService.UpdateOrCreateAsync(key, request.Value, request.Group);
        return Ok(setting);
    }

    [HttpGet("{key}")]
    [Authorize(Roles = "admin")]
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

public class SettingsGroupUpdateRequest
{
    public string Group { get; set; } = null!;
    public Dictionary<string, string?> Values { get; set; } = new();
}
