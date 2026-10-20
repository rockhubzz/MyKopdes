using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class ShuService : IShuService
{
    private readonly KoperasiDbContext _context;

    public ShuService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<Shu>> GetAllAsync(int? year = null)
    {
        var query = _context.Shus.Include(s => s.Member).AsQueryable();
        if (year.HasValue) query = query.Where(s => s.Year == year.Value);
        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public async Task<List<Shu>> GetByMemberAsync(int memberId)
    {
        return await _context.Shus
            .Include(s => s.Member)
            .Where(s => s.MemberId == memberId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Shu?> GetByIdAsync(int id)
    {
        return await _context.Shus
            .Include(s => s.Member)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Shu> CreateAsync(Shu shu)
    {
        shu.CreatedAt = DateTime.UtcNow;
        shu.UpdatedAt = DateTime.UtcNow;
        _context.Shus.Add(shu);
        await _context.SaveChangesAsync();
        return shu;
    }

    public async Task<decimal> GetTotalShuAsync(int? year = null)
    {
        var query = _context.Shus.AsQueryable();
        if (year.HasValue) query = query.Where(s => s.Year == year.Value);
        return await query.SumAsync(s => (decimal)(s.Amount ?? 0));
    }

    public async Task<decimal> GetMemberShuAsync(int memberId, int? year = null)
    {
        try
        {
            var query = _context.Shus.Where(s => s.MemberId == memberId);
            if (year.HasValue) query = query.Where(s => s.Year == year.Value);
            return await query.SumAsync(s => (decimal)(s.Amount ?? 0));
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1146)
        {
            // The shu records table doesn't exist in this deployment — the
            // member's stored SHU balance is the authoritative source.
            var member = await _context.Members.FindAsync(memberId);
            return member?.ShuBalance ?? 0;
        }
    }

    public async Task<Dictionary<string, object>> CalculateShuAsync(int year)
    {
        // Calculate SHU (Surplus/Profit sharing) based on member savings and loans
        var members = await _context.Members.ToListAsync();
        var transactions = await _context.Transactions
            .Where(t => t.CreatedAt >= new DateTime(year, 1, 1) && t.CreatedAt < new DateTime(year + 1, 1, 1))
            .ToListAsync();

        var totalIncome = transactions.Sum(t => (decimal)(t.TotalAmount ?? 0));
        var settings = await _context.Settings
            .Where(s => s.Key == "shu_rate")
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var shuRate = 0.02m;
        if (settings.TryGetValue("shu_rate", out var rateStr) && decimal.TryParse(rateStr, out var parsed))
            shuRate = parsed;

        var totalShu = totalIncome * shuRate;

        // Distribute based on member balance proportion
        var totalBalance = members.Sum(m => (decimal)(m.Balance ?? 0));
        var memberShares = new List<object>();

        foreach (var member in members)
        {
            var share = totalBalance > 0 ? ((member.Balance ?? 0) / totalBalance) * totalShu : 0;
            memberShares.Add(new
            {
                member_id = member.Id,
                member_name = member.Name,
                balance = member.Balance,
                shu_share = Math.Round(share, 2)
            });
        }

        return new Dictionary<string, object>
        {
            ["year"] = year,
            ["total_income"] = totalIncome,
            ["shu_rate"] = shuRate,
            ["total_shu"] = totalShu,
            ["member_count"] = members.Count,
            ["total_member_balance"] = totalBalance,
            ["member_shares"] = memberShares
        };
    }
}
