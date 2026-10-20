using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly KoperasiDbContext _context;

    public UserController(KoperasiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "admin,owner")]
    public async Task<ActionResult<PaginatedResponse<dynamic>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15)
    {
        var query = _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Role,
                u.Phone,
                shift_label = u.ShiftLabel,
                u.IsActive,
                u.CreatedAt
            });

        var total = await query.CountAsync();
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PaginatedResponse<dynamic>
        {
            Data = users.Cast<dynamic>().ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,owner")]
    public async Task<ActionResult<object>> GetUser(int id)
    {
        var u = await _context.Users.FindAsync(id);
        if (u == null) return NotFound(new { message = "User not found" });

        return Ok(new
        {
            u.Id,
            u.Name,
            u.Email,
            u.Role,
            u.Phone,
            shift_label = u.ShiftLabel,
            u.IsActive,
            u.CreatedAt
        });
    }

    [HttpPost]
    [Authorize(Roles = "admin,owner")]
    public async Task<ActionResult<object>> CreateUser([FromBody] UserCreateRequest request)
    {
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existing != null) return BadRequest(new { message = "Email already in use" });

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role ?? "admin",
            Phone = request.Phone,
            ShiftLabel = request.ShiftLabel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Created($"/api/users/{user.Id}", new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.Phone,
            shift_label = user.ShiftLabel
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,owner")]
    public async Task<ActionResult<object>> UpdateUser(int id, [FromBody] UserUpdateRequest request)
    {
        var u = await _context.Users.FindAsync(id);
        if (u == null) return NotFound(new { message = "User not found" });

        u.Name = request.Name ?? u.Name;
        u.Email = request.Email ?? u.Email;
        u.Role = request.Role ?? u.Role;
        u.Phone = request.Phone ?? u.Phone;
        u.ShiftLabel = request.ShiftLabel ?? u.ShiftLabel;
        u.IsActive = request.IsActive ?? u.IsActive;
        u.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Password))
        {
            u.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();
        return Ok(new { u.Id, u.Name, u.Email, u.Role, u.Phone, shift_label = u.ShiftLabel, u.IsActive });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,owner")]
    public async Task<IActionResult> DeleteUser(int id, [FromQuery(Name = "force")] int force = 0)
    {
        var u = await _context.Users.FindAsync(id);
        if (u == null) return NotFound(new { message = "User not found" });

        if (force != 0)
        {
            _context.Users.Remove(u);
        }
        else
        {
            u.IsActive = false;
            u.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "User deleted successfully" });
    }
}

public class UserCreateRequest
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Role { get; set; }
    public string? Phone { get; set; }
    public string? ShiftLabel { get; set; }
}

public class UserUpdateRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    public string? Phone { get; set; }
    public string? ShiftLabel { get; set; }
    public bool? IsActive { get; set; }
}
