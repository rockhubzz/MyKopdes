namespace MyKopdes.Models;

// Maps to MySQL table `shu` (SURPLUS/SHU - return on savings)
public class Shu
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Member Member { get; set; } = null!;
    public decimal? Amount { get; set; }
    public int? Year { get; set; }
    public string? Type { get; set; }            // 'allocation', 'distribution'
    public string? Notes { get; set; }
    public bool IsDistributed { get; set; } = false;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
