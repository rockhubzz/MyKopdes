using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using MyKopdes.Configuration;

namespace MyKopdes.Services;

public class AuthService : IAuthService
{
    private readonly KoperasiDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        KoperasiDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null) return null;

        // Verify bcrypt password hash (same as Laravel's Hash::check)
        if (!VerifyPassword(password, user.Password))
            return null;

        var token = GenerateJwtToken(user.Id, user.Email, user.Role, "user");
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Phone = user.Phone,
                ShiftLabel = user.ShiftLabel,
                Avatar = user.Avatar,
                Locale = user.Locale,
                IsActive = user.IsActive
            }
        };
    }

    public async Task<MemberLoginResponse?> MemberLoginAsync(string identifier, string password)
    {
        var member = await _context.Members
            .FirstOrDefaultAsync(m => (m.Identifier == identifier || m.Phone == identifier || m.Email == identifier) && m.IsActive);

        if (member == null) return null;

        // PIN/password verification: Member password is stored as bcrypt hash
        if (!VerifyPassword(password, member.Password))
            return null;

        var token = GenerateJwtToken(member.Id, member.Identifier, "member", "member");
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        return new MemberLoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Member = new MemberDto
            {
                Id = member.Id,
                Name = member.Name,
                Identifier = member.Identifier,
                Email = member.Email,
                Phone = member.Phone,
                Avatar = member.Avatar,
                Locale = member.Locale,
                JoinDate = member.JoinDate,
                ShuBalance = member.ShuBalance,
                IsActive = member.IsActive
            }
        };
    }

    public async Task<bool> IsMemberPinValidAsync(int memberId, string pin)
    {
        var passwordHash = await _context.Database
            .SqlQueryRaw<string>("SELECT password FROM members WHERE id = {0}", memberId)
            .FirstOrDefaultAsync();

        if (passwordHash == null) return false;
        return VerifyPassword(pin, passwordHash);
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<Member?> GetMemberByIdentifierAsync(string identifier)
    {
        return await _context.Members.FirstOrDefaultAsync(m => m.Identifier == identifier || m.Phone == identifier);
    }

    public async Task UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    private string GenerateJwtToken(int userId, string identifier, string role, string type)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, identifier),
            new(ClaimTypes.Role, role),
            new("type", type),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
