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
        if (member.JoinDate == null)
            member.JoinDate = DateTime.UtcNow;

        // Hash the password with bcrypt (same scheme AuthService verifies at
        // member login). The admin members UI sends a plaintext password on
        // create — storing it as-is would make the member unable to log in.
        if (!string.IsNullOrEmpty(member.Password) && !member.Password.StartsWith("$2"))
        {
            member.Password = BCrypt.Net.BCrypt.HashPassword(member.Password);
        }

        // Generate membership_id if not provided. Derive the next number from
        // the highest existing identifier with the same yyMM prefix — counting
        // today's rows collides with existing IDs whenever members were seeded
        // on the same day (duplicate-key 500).
        if (string.IsNullOrEmpty(member.Identifier))
        {
            var prefix = $"KOP-{DateTime.UtcNow:yyMM}-";
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var last = await _context.Members
                    .Where(m => m.Identifier.StartsWith(prefix))
                    .OrderByDescending(m => m.Identifier)
                    .Select(m => m.Identifier)
                    .FirstOrDefaultAsync();
                var nextNumber = last != null && int.TryParse(last.AsSpan(prefix.Length), out var n) ? n + 1 : 1;
                member.Identifier = $"{prefix}{nextNumber:D4}";

                try
                {
                    member.CreatedAt = DateTime.UtcNow;
                    member.UpdatedAt = DateTime.UtcNow;
                    _context.Members.Add(member);
                    await _context.SaveChangesAsync();
                    return member;
                }
                catch (DbUpdateException ex) when (IsDuplicateKey(ex))
                {
                    _context.Entry(member).State = EntityState.Detached;
                    if (attempt == 2) throw;
                }
            }
        }

        member.CreatedAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;
        _context.Members.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    private static bool IsDuplicateKey(DbUpdateException ex)
        => ex.InnerException is MySqlConnector.MySqlException { Number: 1062 };

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
        // The members UI sends a password only when the admin sets a new one
        // (empty = keep the current). Hash it like at create — member login
        // verifies with bcrypt.
        if (!string.IsNullOrEmpty(member.Password))
        {
            existing.Password = member.Password.StartsWith("$2")
                ? member.Password
                : BCrypt.Net.BCrypt.HashPassword(member.Password);
        }
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
