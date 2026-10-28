using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

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
        var phone = request.Phone!.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var requestedIdentifier = string.IsNullOrWhiteSpace(request.Identifier) ? null : request.Identifier.Trim();

        if (await _memberService.GetByIdentifierAsync(phone) != null ||
            (requestedIdentifier != null && await _memberService.GetByIdentifierAsync(requestedIdentifier) != null))
            return Conflict(new { message = "A member with this phone or membership ID already exists" });

        if (email != null && await _memberService.GetByEmailAsync(email) != null)
            return Conflict(new { message = "A member with this email already exists" });

        var member = new MemberModel
        {
            Name = request.Name.Trim(),
            // Leave this empty for self-signup so MemberService generates the
            // official KOP-yyMM-NNNN membership ID atomically.
            Identifier = requestedIdentifier,
            Phone = phone,
            Address = request.Address,
            Email = email,
            Password = request.Password,
            ShuBalance = 0,
            IsActive = true,
            Locale = "en",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        MemberModel created;
        try
        {
            created = await _memberService.CreateAsync(member);
        }
        catch (DbUpdateException)
        {
            // Keep unique phone/email races user-readable instead of exposing
            // a database error if two signups arrive at the same time.
            return Conflict(new { message = "A member with these details already exists" });
        }

        var result = await _authService.MemberLoginAsync(created.Identifier!, request.Password);
        if (result == null)
            return BadRequest(new { message = "Failed to login after registration" });
        return Ok(result);
    }

    [HttpGet("me")]
    [HttpPost("me")] // legacy .NET client compatibility; Laravel uses GET
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
    [Required, StringLength(255, MinimumLength = 2)]
    public string Name { get; set; } = null!;
    public string? Identifier { get; set; }
    [Required, StringLength(255, MinimumLength = 8)]
    public string Password { get; set; } = null!;
    [Required, StringLength(255, MinimumLength = 6)]
    public string Phone { get; set; } = null!;
    [StringLength(1000)]
    public string? Address { get; set; }
    [EmailAddress, StringLength(255)]
    public string? Email { get; set; }
}
