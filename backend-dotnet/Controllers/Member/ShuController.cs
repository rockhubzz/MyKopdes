using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Member;

[ApiController]
[Route("api/member")]
public class ShuController : ControllerBase
{
    private readonly IShuService _shuService;
    private readonly KoperasiDbContext _context;

    public ShuController(IShuService shuService, KoperasiDbContext context)
    {
        _shuService = shuService;
        _context = context;
    }

    [HttpGet("shu")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<object>> GetShu()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var member = await _context.Members.FindAsync(userId.Value);
        if (member == null) return NotFound(new { message = "Member not found" });

        // The frontend member SHU page expects { shu_balance, accrual_note }.
        // SHU balance lives on members.shu_balance (the shu distribution
        // records table is not part of this deployment's schema).
        var shuRate = await GetShuRateAsync();
        return Ok(new
        {
            shu_balance = member.ShuBalance,
            accrual_note = $"SHU is shared annually out of the cooperative surplus in proportion to your savings balance (current rate: {shuRate * 100m:0.##}%)."
        });
    }

    private async Task<decimal> GetShuRateAsync()
    {
        var value = await _context.Settings
            .AsNoTracking()
            .Where(s => s.Key == "shu_rate")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        return decimal.TryParse(value ?? "0.02", out var rate) ? rate : 0.02m;
    }

    [HttpGet("shu/balance")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<object>> GetShuBalance([FromQuery] int? year = null)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var balance = await _shuService.GetMemberShuAsync(userId.Value, year);
        return Ok(new { balance, year = year ?? DateTime.UtcNow.Year });
    }

    [HttpGet("discounts")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<IEnumerable<Discount>>> GetMemberDiscounts()
    {
        var now = DateTime.UtcNow;
        var discounts = await _context.Discounts
            .Where(d => (d.Scope == "member" || d.Scope == "general") && d.IsActive &&
                (d.StartDate == null || d.StartDate <= now) &&
                (d.EndDate == null || d.EndDate >= now))
            .Include(d => d.Category)
            .ToListAsync();
        return Ok(discounts);
    }
}
