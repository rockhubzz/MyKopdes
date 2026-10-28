using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IMemberService
{
    Task<List<Member>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null, bool activeOnly = true);
    Task<Member?> GetByIdAsync(int id);
    Task<Member?> GetByIdentifierAsync(string identifier);
    Task<Member?> GetByEmailAsync(string email);
    Task<Member> CreateAsync(Member member);
    Task<Member> UpdateAsync(int id, Member member);
    Task<bool> DeleteAsync(int id, bool force = false);
    Task<List<Member>> SearchAsync(string query);
    Task<List<Member>> LookupAsync(string query);
    Task<int> GetTotalCountAsync(string? search = null, bool activeOnly = true);
    Task<Member?> GetByMemberIdAsync(int memberId);
    Task<Member> UpdateBalanceAsync(int memberId, decimal amount);
    Task SetMemberPinAsync(int memberId, string pinHash);
}
