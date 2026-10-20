using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace MyKopdes.Controllers;

using MemberModel = MyKopdes.Models.Member;

[ApiController]
[Route("api/auth/member")]
public class MemberAuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMemberService _memberService;

    public MemberAuthController(IAuthService authService, IMemberService memberService)
    {
        _authService = authService;
        _memberService = memberService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<MemberLoginResponse>> Login([FromBody] MemberLoginRequest request)
    {
        var result = await _authService.MemberLoginAsync(request.Identifier, request.Password);
        if (result == null)
            return Unauthorized(new { message = "Invalid credentials" });
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<MemberLoginResponse>> Register([FromBody] MemberRegisterRequest request)
    {
        var existing = await _memberService.GetByIdentifierAsync(request.Identifier);
        if (existing != null)
            return BadRequest(new { message = "Member with this identifier already exists" });

        var pinHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var member = new MemberModel
        {
            Name = request.Name,
            Identifier = request.Identifier,
            Phone = request.Phone,
            Address = request.Address,
            Email = request.Email,
            Password = pinHash,
            ShuBalance = 0,
            IsActive = true,
            Locale = "en",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _memberService.CreateAsync(member);
        var result = await _authService.MemberLoginAsync(request.Identifier, request.Password);
        if (result == null)
            return BadRequest(new { message = "Failed to login after registration" });
        return Ok(result);
    }

    [HttpPost("me")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<object>> Me()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { message = "Member not authenticated" });

        var member = await _memberService.GetByMemberIdAsync(userId.Value);
        if (member == null)
            return NotFound(new { message = "Member not found" });

        return Ok(new
        {
            id = member.Id,
            name = member.Name,
            identifier = member.Identifier,
            phone = member.Phone,
            address = member.Address,
            email = member.Email,
            balance = member.Balance,
            loan_balance = member.LoanBalance
        });
    }
}

public class MemberRegisterRequest
{
    public string Name { get; set; } = null!;
    public string Identifier { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
}
