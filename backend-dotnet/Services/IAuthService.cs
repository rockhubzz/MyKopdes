using MyKopdes.Models;
using MyKopdes.Models.DTOs;

namespace MyKopdes.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string email, string password);
    Task<MemberLoginResponse?> MemberLoginAsync(string identifier, string password);
    Task<User?> GetUserByIdAsync(int id);
    Task<Member?> GetMemberByIdentifierAsync(string identifier);
    Task<bool> IsMemberPinValidAsync(int memberId, string pin);
    Task UpdateUserAsync(User user);
}
