using Microsoft.AspNetCore.Mvc;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;
using Microsoft.AspNetCore.Authorization;

namespace MyKopdes.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IFileUploadService _fileUploads;

    public AuthController(IAuthService authService, IFileUploadService fileUploads)
    {
        _authService = authService;
        _fileUploads = fileUploads;
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

        if (!string.IsNullOrEmpty(form["name"])) user.Name = form["name"];
        if (!string.IsNullOrEmpty(form["phone"])) user.Phone = form["phone"];
        if (!string.IsNullOrEmpty(form["shift_label"])) user.ShiftLabel = form["shift_label"];

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
    public IActionResult StaffLogout() => Ok(new { message = "Logged out" });

    // Member logout
    [HttpPost("member/logout")]
    public IActionResult MemberLogout() => Ok(new { message = "Logged out" });
}
