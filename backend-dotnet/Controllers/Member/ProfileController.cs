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

    public MemberProfileController(IMemberService memberService, KoperasiDbContext context)
    {
        _memberService = memberService;
        _context = context;
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

        var today = DateTime.UtcNow.Date;
        var transactions = await _context.Transactions
            .Where(t => t.MemberId == member.Id && t.CreatedAt >= today && t.CreatedAt < today.AddDays(1))
            .ToListAsync();

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
            .Where(t => t.MemberId == member.Id)
            .CountAsync();

        var totalSpent = await _context.Transactions
            .Where(t => t.MemberId == member.Id)
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

        if (!string.IsNullOrEmpty(form["name"])) member.Name = form["name"];
        if (!string.IsNullOrEmpty(form["phone"])) member.Phone = form["phone"];
        if (!string.IsNullOrEmpty(form["address"])) member.Address = form["address"];
        if (!string.IsNullOrEmpty(form["email"])) member.Email = form["email"];
        if (!string.IsNullOrEmpty(form["locale"])) member.Locale = form["locale"];
        if (!string.IsNullOrEmpty(form["password"]))
        {
            member.Password = BCrypt.Net.BCrypt.HashPassword(form["password"]);
        }

        // Avatar upload
        var avatar = form.Files.FirstOrDefault();
        if (avatar != null && avatar.Length > 0)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "storage", "avatars");
            Directory.CreateDirectory(uploadsDir);
            var filename = $"member_{member.Id}_{DateTime.UtcNow.Ticks}{Path.GetExtension(avatar.FileName)}";
            var filepath = Path.Combine(uploadsDir, filename);
            using (var stream = new FileStream(filepath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }
            // Remove old avatar
            if (!string.IsNullOrEmpty(member.Avatar))
            {
                var oldPath = Path.Combine(uploadsDir, member.Avatar);
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
            member.Avatar = filename;
        }
        else if (form["remove_avatar"] == "1")
        {
            if (!string.IsNullOrEmpty(member.Avatar))
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "storage", "avatars");
                var oldPath = Path.Combine(uploadsDir, member.Avatar);
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
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
