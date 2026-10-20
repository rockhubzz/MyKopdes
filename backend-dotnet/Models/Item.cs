namespace MyKopdes.Models;

// Maps to MySQL table `items`
public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    [System.Text.Json.Serialization.JsonPropertyName("sku")]
    public string Code { get; set; } = null!;       // sku
    public string? Barcode { get; set; }
    public int? CategoryId { get; set; }
    public ItemCategory? Category { get; set; }
    public int? SupplierId { get; set; }              // not in DB; virtual
    public Supplier? Supplier { get; set; }            // not in DB; virtual
    [System.Text.Json.Serialization.JsonPropertyName("unit_price")]
    public decimal? Price { get; set; }              // unit_price
    [System.Text.Json.Serialization.JsonPropertyName("cost_price")]
    public decimal? Cost { get; set; }               // cost_price
    [System.Text.Json.Serialization.JsonPropertyName("current_stock")]
    public int? Stock { get; set; }                  // current_stock
    [System.Text.Json.Serialization.JsonPropertyName("min_stock_threshold")]
    public int? MinStock { get; set; }               // min_stock_threshold
    [System.Text.Json.Serialization.JsonPropertyName("unit_of_measure")]
    public string Unit { get; set; } = "pcs";       // unit_of_measure
    [System.Text.Json.Serialization.JsonPropertyName("expiry_date")]
    public DateTime? ExpiryDate { get; set; }       // expiry_date
    public bool IsActive { get; set; } = true;
    [System.Text.Json.Serialization.JsonIgnore]
    public string? Description { get; set; }        // not in DB; ignored
    [System.Text.Json.Serialization.JsonPropertyName("image_path")]
    public string? Image { get; set; }              // image_path
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<TransactionItem>? TransactionItems { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<RestockingRecord>? RestockingRecords { get; set; }
}
