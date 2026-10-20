namespace MyKopdes.Models;

// Member: cooperative members (from Laravel App\Models\Member)
// Maps to MySQL table `members`
public class Member
{
    public int Id { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("membership_id")]
    public string? Identifier { get; set; }  // membership_id, e.g. KOP-26-IASQV
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string Password { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;
    [System.Text.Json.Serialization.JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("avatar_path")]
    public string? Avatar { get; set; }            // avatar_path
    [System.Text.Json.Serialization.JsonPropertyName("locale")]
    public string? Locale { get; set; } = "en";    // 'en' or 'id'
    [System.Text.Json.Serialization.JsonPropertyName("join_date")]
    public DateTime? JoinDate { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("shu_balance")]
    public decimal ShuBalance { get; set; } = 0;    // shu_balance
    [System.Text.Json.Serialization.JsonPropertyName("balance")]
    public decimal? Balance { get; set; }            // computed/virtual — no DB column
    [System.Text.Json.Serialization.JsonPropertyName("loan_balance")]
    public decimal? LoanBalance { get; set; }        // computed/virtual — no DB column
    [System.Text.Json.Serialization.JsonIgnore]
    public string? RememberToken { get; set; }

    // Navigation properties
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<Transaction>? Transactions { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<Loan>? Loans { get; set; }
}
