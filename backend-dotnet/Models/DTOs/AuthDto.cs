namespace MyKopdes.Models.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class MemberLoginRequest
{
    public string Identifier { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class MemberLoginResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public MemberDto Member { get; set; } = null!;
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? Phone { get; set; }
    public string? ShiftLabel { get; set; }
    public string? Avatar { get; set; }
    public string? Locale { get; set; }
    public bool IsActive { get; set; }
}

public class MemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    // Same key as /member/profile and the frontend Member type.
    [System.Text.Json.Serialization.JsonPropertyName("membership_id")]
    public string Identifier { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("avatar_path")]
    public string? Avatar { get; set; }
    public string? Locale { get; set; }
    public DateTime? JoinDate { get; set; }
    public decimal ShuBalance { get; set; }
    public bool IsActive { get; set; }
}

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    [System.Text.Json.Serialization.JsonPropertyName("current_page")]
    public int Page { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("per_page")]
    public int PageSize { get; set; }
    public int Total { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("last_page")]
    public int TotalPages { get; set; }
}
