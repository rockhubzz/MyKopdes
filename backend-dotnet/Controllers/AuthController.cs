using Microsoft.AspNetCore.Mvc;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;

namespace MyKopdes.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IFileUploadService _fileUploads;
    private readonly KoperasiDbContext _context;

    public AuthController(IAuthService authService, IFileUploadService fileUploads, KoperasiDbContext context)
    {
        _authService = authService;
        _fileUploads = fileUploads;
        _context = context;
    }

    // Staff login — matches frontend /api/auth/staff/login
    [HttpPost("staff/login")]
    public async Task<ActionResult<LoginResponse>> StaffLogin([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);
        if (result == null)
            return Unauthorized(new { message = "Invalid credentials" });
        return Ok(result);
    }

    // Member login — handled by MemberAuthController at /api/auth/member/login

    // Staff profile — GET
    [HttpGet("staff/profile")]
    [Authorize(Roles = "admin,shop_owner,employee")]
    public async Task<ActionResult<object>> StaffProfile()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "User not authenticated" });
        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role,
            phone = user.Phone,
            shift_label = user.ShiftLabel,
            avatar_path = user.Avatar,
            locale = user.Locale,
            is_active = user.IsActive
        });
    }

    // Staff profile — POST (multipart form, updates name/phone/avatar/shift_label)
    [HttpPost("staff/profile")]
    [Authorize(Roles = "admin,shop_owner,employee")]
    public async Task<ActionResult<object>> UpdateStaffProfile()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "User not authenticated" });

        var form = await Request.ReadFormAsync();
        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null) return NotFound(new { message = "User not found" });

        if (form.ContainsKey("name")) user.Name = form["name"].ToString().Trim();
        if (form.ContainsKey("email"))
        {
            var email = form["email"].ToString().Trim();
            var duplicate = await _context.Users.AnyAsync(u => u.Id != user.Id && u.Email == email);
            if (duplicate) return BadRequest(new { message = "Email already in use" });
            user.Email = email;
        }
        if (form.ContainsKey("phone")) user.Phone = string.IsNullOrWhiteSpace(form["phone"].ToString()) ? null : form["phone"].ToString();
        if (form.ContainsKey("shift_label")) user.ShiftLabel = string.IsNullOrWhiteSpace(form["shift_label"].ToString()) ? null : form["shift_label"].ToString();
        if (form.ContainsKey("locale") && (form["locale"] == "en" || form["locale"] == "id")) user.Locale = form["locale"];
        if (!string.IsNullOrEmpty(form["password"]))
        {
            if (form["password"].ToString().Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters" });
            user.Password = BCrypt.Net.BCrypt.HashPassword(form["password"]);
        }

        // Avatar upload — stored as a storage-relative path so the frontend's
        // /storage/{avatar_path} URL resolves (static-files middleware).
        var avatar = form.Files.FirstOrDefault();
        if (avatar != null && avatar.Length > 0)
        {
            var saved = await _fileUploads.SaveImageAsync(avatar, "avatars", $"user_{user.Id}");
            if (saved == null)
                return BadRequest(new { message = "Unsupported image type or file too large (jpg/png/webp/gif, max 10MB)" });
            if (!string.IsNullOrEmpty(user.Avatar)) _fileUploads.DeleteStoredImage(user.Avatar);
            user.Avatar = saved;
        }
        else if (form["remove_avatar"] == "1" || form["remove_avatar"] == "true")
        {
            _fileUploads.DeleteStoredImage(user.Avatar);
            user.Avatar = null;
        }

        await _authService.UpdateUserAsync(user);
        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role,
            phone = user.Phone,
            shift_label = user.ShiftLabel,
            avatar_path = user.Avatar,
            locale = user.Locale,
            is_active = user.IsActive
        });
    }

    // Staff me — alias for profile
    [HttpGet("staff/me")]
    [Authorize(Roles = "admin,shop_owner,employee")]
    public async Task<ActionResult<object>> StaffMe()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "User not authenticated" });
        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            role = user.Role,
            phone = user.Phone,
            shift_label = user.ShiftLabel,
            avatar_path = user.Avatar,
            locale = user.Locale,
            is_active = user.IsActive
        });
    }

    // Staff logout
    [HttpPost("staff/logout")]
    [Authorize(Roles = "admin,shop_owner,employee")]
    public IActionResult StaffLogout() => Ok(new { message = "Logged out" });

    // Member logout
    [HttpPost("member/logout")]
    [Authorize(Roles = "member")]
    public IActionResult MemberLogout() => Ok(new { message = "Logged out" });
}
