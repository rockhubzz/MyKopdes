using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/staff")]
public class ProfileController : ControllerBase
{
    private readonly KoperasiDbContext _context;

    public ProfileController(KoperasiDbContext context)
    {
        _context = context;
    }

    [HttpGet("profile")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<object>> GetProfile()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return NotFound(new { message = "User not found" });

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.Phone,
            user.Avatar,
            user.Locale
        });
    }

    [HttpPut("profile")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<object>> UpdateProfile([FromBody] StaffProfileUpdate request)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return NotFound(new { message = "User not found" });

        if (!string.IsNullOrEmpty(request.Name)) user.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Phone)) user.Phone = request.Phone;
        if (!string.IsNullOrEmpty(request.Avatar)) user.Avatar = request.Avatar;
        if (!string.IsNullOrEmpty(request.Locale)) user.Locale = request.Locale;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.Phone,
            user.Avatar,
            user.Locale
        });
    }
}

public class StaffProfileUpdate
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string? Locale { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}
