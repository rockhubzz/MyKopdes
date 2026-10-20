using System.Text.Json.Serialization;

namespace MyKopdes.Models;

// User: staff/admin users table (from Laravel App\Models\User)
// Maps to MySQL table `users`
public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    [JsonIgnore]
    public string Password { get; set; } = null!;  // bcrypt hash

    [JsonPropertyName("role")]
    public string Role { get; set; } = "admin";    // admin, shop_owner, employee

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("shift_label")]
    public string? ShiftLabel { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("email_verified_at")]
    public DateTime? EmailVerifiedAt { get; set; }

    [JsonPropertyName("avatar_path")]
    public string? Avatar { get; set; }            // avatar_path

    [JsonPropertyName("locale")]
    public string? Locale { get; set; } = "en";    // 'en' or 'id'

    [JsonIgnore]
    public string? RememberToken { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [JsonIgnore]
    public ICollection<Transaction>? Transactions { get; set; }
    [JsonIgnore]
    public ICollection<RestockingRecord>? RestockingRecords { get; set; }
    [JsonIgnore]
    public ICollection<Loan>? Loans { get; set; }
}
