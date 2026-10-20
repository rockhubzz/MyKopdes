namespace MyKopdes.Models;

// Maps to MySQL table `loan_settings`
public class LoanSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public string? Type { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
