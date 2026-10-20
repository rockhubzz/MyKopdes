namespace MyKopdes.Models;

// Maps to MySQL table `restocking_records`
public class RestockingRecord
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Item Item { get; set; } = null!;
    public int? SupplierId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Supplier? Supplier { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("submitted_by")]
    public int? UserId { get; set; }              // submitted_by
    [System.Text.Json.Serialization.JsonIgnore]
    public User? User { get; set; }
    public int Quantity { get; set; }
    public decimal? CostPerUnit { get; set; }
    public decimal? TotalCost { get; set; }       // not directly a column; computed
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int? TransactionId { get; set; }
    public string? Notes { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("restocked_at")]
    public DateTime? RestockDate { get; set; }    // restocked_at
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
