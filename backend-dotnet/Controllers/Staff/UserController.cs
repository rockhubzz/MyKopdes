using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Middleware;

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
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PaginatedResponse<dynamic>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? search = null,
        [FromQuery(Name = "include_inactive")] bool includeInactive = false,
        [FromQuery] string? role = null)
    {
        var query = _context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role);
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.Name.Contains(search) || u.Email.Contains(search) ||
                                     (u.Phone != null && u.Phone.Contains(search)));
        }
        if (!includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }

        var total = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.Name)
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
            })
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
    [Authorize(Roles = "admin")]
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
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<object>> CreateUser([FromBody] UserCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Role) || !new[] { "admin", "shop_owner", "employee" }.Contains(request.Role))
            return BadRequest(new { message = "A valid role is required" });
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
    [Authorize(Roles = "admin")]
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
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteUser(int id, [FromQuery(Name = "force")] int force = 0)
    {
        var u = await _context.Users.FindAsync(id);
        if (u == null) return NotFound(new { message = "User not found" });
        if (User.GetUserId() == id)
            return UnprocessableEntity(new { message = "You cannot deactivate your own account" });

        if (force != 0)
        {
            if (await _context.Transactions.AnyAsync(t => t.UserId == id) ||
                await _context.RestockingRecords.AnyAsync(r => r.UserId == id))
                return UnprocessableEntity(new { message = "Cannot permanently delete a user with transaction or restock history" });
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
    [System.ComponentModel.DataAnnotations.MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [System.ComponentModel.DataAnnotations.Required]
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
