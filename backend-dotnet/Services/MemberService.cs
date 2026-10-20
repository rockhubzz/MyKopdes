using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class MemberService : IMemberService
{
    private readonly KoperasiDbContext _context;

    public MemberService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<Member>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null)
    {
        var query = _context.Members.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.Name.Contains(search) || m.Identifier.Contains(search) || (m.Phone != null && m.Phone.Contains(search)));
        }
        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Member?> GetByIdAsync(int id)
    {
        return await _context.Members.FindAsync(id);
    }

    public async Task<Member?> GetByIdentifierAsync(string identifier)
    {
        return await _context.Members.FirstOrDefaultAsync(m => m.Identifier == identifier || m.Phone == identifier);
    }

    public async Task<Member> CreateAsync(Member member)
    {
        // Generate membership_id if not provided
        if (string.IsNullOrEmpty(member.Identifier))
        {
            var today = DateTime.UtcNow.ToString("yyMM");
            var count = await _context.Members.CountAsync(m => m.CreatedAt >= DateTime.UtcNow.Date);
            var nextNumber = count + 1;
            member.Identifier = $"KOP-{today}-{nextNumber:D4}";
        }

        if (member.JoinDate == null)
            member.JoinDate = DateTime.UtcNow;
        member.IsActive = member.IsActive;
        member.CreatedAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;
        _context.Members.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task<Member> UpdateAsync(int id, Member member)
    {
        var existing = await _context.Members.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException("Member not found");

        existing.Name = member.Name;
        existing.Phone = member.Phone;
        existing.Address = member.Address;
        existing.Email = member.Email;
        existing.IsActive = member.IsActive;
        existing.Avatar = member.Avatar;
        existing.Locale = member.Locale;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.Members.Update(existing);
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, bool force = false)
    {
        var member = await _context.Members.FindAsync(id);
        if (member == null) return false;

        if (force)
        {
            _context.Members.Remove(member);
        }
        else
        {
            member.IsActive = false;
            member.UpdatedAt = DateTime.UtcNow;
            _context.Members.Update(member);
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Member>> SearchAsync(string query)
    {
        return await _context.Members
            .Where(m => m.Name.Contains(query) || m.Identifier.Contains(query) || (m.Phone != null && m.Phone.Contains(query)))
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? search = null)
    {
        var query = _context.Members.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.Name.Contains(search) || m.Identifier.Contains(search) || (m.Phone != null && m.Phone.Contains(search)));
        }
        return await query.CountAsync();
    }

    public async Task<Member?> GetByMemberIdAsync(int memberId)
    {
        return await _context.Members.FindAsync(memberId);
    }

    public async Task<Member> UpdateBalanceAsync(int memberId, decimal amount)
    {
        var member = await _context.Members.FindAsync(memberId);
        if (member == null) throw new KeyNotFoundException("Member not found");
        member.Balance += amount;
        member.UpdatedAt = DateTime.UtcNow;
        _context.Members.Update(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task SetMemberPinAsync(int memberId, string pinHash)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE members SET password = {0} WHERE id = {1}", pinHash, memberId);
    }
}
