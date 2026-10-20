namespace MyKopdes.Models;

// Maps to MySQL table `transaction_items`
public class TransactionItem
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Transaction Transaction { get; set; } = null!;
    public int ItemId { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("item")]
    public Item Item { get; set; } = null!;
    public int Quantity { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("unit_price")]
    public decimal? Price { get; set; }            // unit_price
    public decimal? Subtotal { get; set; }         // subtotal
    public decimal? DiscountAmount { get; set; }    // not in DB
    public DateTime? CreatedAt { get; set; }
}
