using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Services;
using MyKopdes.Middleware;
using System.Security.Claims;

namespace MyKopdes.Controllers.Member;

[ApiController]
[Route("api/member")]
public class MemberProfileController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly KoperasiDbContext _context;
    private readonly IFileUploadService _fileUploads;

    public MemberProfileController(IMemberService memberService, KoperasiDbContext context, IFileUploadService fileUploads)
    {
        _memberService = memberService;
        _context = context;
        _fileUploads = fileUploads;
    }

    [HttpGet("me")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<object>> GetMe()
    {
        // The member layout revalidates its cached profile against this
        // endpoint on every mount; same shape as /member/profile.
        return await GetProfile();
    }

    [HttpGet("profile")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<object>> GetProfile()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var member = await _memberService.GetByMemberIdAsync(userId.Value);
        if (member == null) return NotFound(new { message = "Member not found" });

        return Ok(new
        {
            id = member.Id,
            membership_id = member.Identifier,
            name = member.Name,
            email = member.Email,
            phone = member.Phone,
            address = member.Address,
            avatar_path = member.Avatar,
            locale = member.Locale ?? "id",
            join_date = member.JoinDate?.ToString("yyyy-MM-dd"),
            shu_balance = member.ShuBalance,
            balance = member.Balance,
            loan_balance = member.LoanBalance,
            is_active = member.IsActive
        });
    }

    [HttpGet("dashboard-summary")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<object>> GetDashboardSummary()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var member = await _memberService.GetByMemberIdAsync(userId.Value);
        if (member == null) return NotFound(new { message = "Member not found" });

        var recentTransactions = await _context.Transactions
            .Where(t => t.MemberId == member.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new
            {
                id = t.Id,
                transaction_code = t.TransactionNumber,
                total = t.TotalAmount,
                payment_method = t.PaymentMethod,
                payment_status = t.PaymentStatus,
                created_at = t.CreatedAt
            })
            .ToListAsync();

        var totalPurchases = await _context.Transactions
            .Where(t => t.MemberId == member.Id && t.PaymentStatus == "paid")
            .CountAsync();

        var totalSpent = await _context.Transactions
            .Where(t => t.MemberId == member.Id && t.PaymentStatus == "paid")
            .SumAsync(t => (decimal)(t.TotalAmount ?? 0));

        return Ok(new
        {
            shu_balance = member.ShuBalance,
            total_purchases = totalPurchases,
            total_spent = totalSpent,
            recent_transactions = recentTransactions
        });
    }

    // POST used by the frontend (multipart form-data)
    [HttpPost("profile")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<object>> UpdateProfilePost()
    {
        return await UpdateProfileInternal();
    }

    // PUT also supported for API consistency
    [HttpPut("profile")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<object>> UpdateProfilePut()
    {
        return await UpdateProfileInternal();
    }

    private async Task<ActionResult<object>> UpdateProfileInternal()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var member = await _memberService.GetByMemberIdAsync(userId.Value);
        if (member == null) return NotFound(new { message = "Member not found" });

        var form = await Request.ReadFormAsync();

        if (form.ContainsKey("name")) member.Name = form["name"].ToString().Trim();
        if (form.ContainsKey("phone"))
        {
            var phone = form["phone"].ToString().Trim();
            if (!string.IsNullOrWhiteSpace(phone) && await _context.Members.AnyAsync(m => m.Id != member.Id && m.Phone == phone))
                return Conflict(new { message = "Phone already in use" });
            member.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
        }
        if (form.ContainsKey("address")) member.Address = string.IsNullOrWhiteSpace(form["address"].ToString()) ? null : form["address"].ToString();
        if (form.ContainsKey("email"))
        {
            var email = form["email"].ToString().Trim();
            if (!string.IsNullOrWhiteSpace(email) && await _context.Members.AnyAsync(m => m.Id != member.Id && m.Email == email))
                return Conflict(new { message = "Email already in use" });
            member.Email = string.IsNullOrWhiteSpace(email) ? null : email;
        }
        if (form.ContainsKey("locale") && (form["locale"] == "en" || form["locale"] == "id")) member.Locale = form["locale"];
        if (!string.IsNullOrEmpty(form["password"]))
        {
            if (form["password"].ToString().Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters" });
            member.Password = BCrypt.Net.BCrypt.HashPassword(form["password"]);
        }

        // Avatar upload — stored as a storage-relative path so the frontend's
        // /storage/{avatar_path} URL resolves (static-files middleware).
        var avatar = form.Files.FirstOrDefault();
        if (avatar != null && avatar.Length > 0)
        {
            var saved = await _fileUploads.SaveImageAsync(avatar, "avatars", $"member_{member.Id}");
            if (saved == null)
                return BadRequest(new { message = "Unsupported image type or file too large (jpg/png/webp/gif, max 10MB)" });
            _fileUploads.DeleteStoredImage(member.Avatar);
            member.Avatar = saved;
        }
        else if (form["remove_avatar"] == "1" || form["remove_avatar"] == "true")
        {
            _fileUploads.DeleteStoredImage(member.Avatar);
            member.Avatar = null;
        }

        member.UpdatedAt = DateTime.UtcNow;

        await _memberService.UpdateAsync(member.Id, member);

        return Ok(new
        {
            id = member.Id,
            membership_id = member.Identifier,
            name = member.Name,
            email = member.Email,
            phone = member.Phone,
            address = member.Address,
            avatar_path = member.Avatar,
            locale = member.Locale ?? "id",
            join_date = member.JoinDate?.ToString("yyyy-MM-dd"),
            shu_balance = member.ShuBalance,
            balance = member.Balance,
            loan_balance = member.LoanBalance,
            is_active = member.IsActive
        });
    }
}
