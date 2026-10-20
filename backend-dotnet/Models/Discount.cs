namespace MyKopdes.Models;

// Maps to MySQL table `discounts`
public class Discount
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    [System.Text.Json.Serialization.JsonIgnore]
    public string? Description { get; set; }
    public int? ItemId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Item? Item { get; set; }
    public int? CategoryId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public ItemCategory? Category { get; set; }
    public int? MemberId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Member? Member { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public decimal DiscountRate { get; set; }   // maps to `value`
    [System.Text.Json.Serialization.JsonPropertyName("buy_qty")]
    public int? BuyQty { get; set; }            // buy_qty
    [System.Text.Json.Serialization.JsonPropertyName("get_qty")]
    public int? GetQty { get; set; }            // get_qty
    [System.Text.Json.Serialization.JsonPropertyName("min_purchase")]
    public decimal? MinPurchase { get; set; }   // min_purchase
    [System.Text.Json.Serialization.JsonPropertyName("discount_amount")]
    public decimal? DiscountAmount { get; set; }
    public string Type { get; set; } = "percentage";  // maps to `type` (percentage, flat, buy_x_get_y)
    [System.Text.Json.Serialization.JsonPropertyName("starts_at")]
    public DateTime? StartDate { get; set; }          // starts_at
    [System.Text.Json.Serialization.JsonPropertyName("ends_at")]
    public DateTime? EndDate { get; set; }            // ends_at
    [System.Text.Json.Serialization.JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Scope { get; set; } = "general";
}
