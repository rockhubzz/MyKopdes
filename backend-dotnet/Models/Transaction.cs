namespace MyKopdes.Models;

// Maps to MySQL table `transactions`
public class Transaction
{
    public int Id { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("transaction_code")]
    public string TransactionNumber { get; set; } = null!;  // transaction_code
    public int? MemberId { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("member")]
    public Member? Member { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("cashier_id")]
    public int? UserId { get; set; }              // cashier_id
    [System.Text.Json.Serialization.JsonPropertyName("cashier")]
    public User? User { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string Type { get; set; } = null!;     // not in DB; for API compatibility
    public decimal? Subtotal { get; set; }         // subtotal
    [System.Text.Json.Serialization.JsonPropertyName("discount_amount")]
    public decimal? DiscountAmount { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("tax_amount")]
    public decimal? TaxAmount { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("total")]
    public decimal? TotalAmount { get; set; }     // total
    [System.Text.Json.Serialization.JsonPropertyName("payment_method")]
    public string? PaymentMethod { get; set; }     // payment_method
    [System.Text.Json.Serialization.JsonPropertyName("payment_status")]
    public string? PaymentStatus { get; set; }     // payment_status
    [System.Text.Json.Serialization.JsonPropertyName("paid_at")]
    public DateTime? PaidAt { get; set; }          // paid_at
    [System.Text.Json.Serialization.JsonIgnore]
    public decimal? PaidAmount { get; set; }      // not a column; computed = total
    [System.Text.Json.Serialization.JsonIgnore]
    public string? Notes { get; set; }     // not in DB; ignored
    [System.Text.Json.Serialization.JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    public List<TransactionItem> Items { get; set; } = new();
}
