namespace MyKopdes.Models;

// Stock opname / inventory adjustment record
// Maps to MySQL table `stock_opnames`
public class StockOpname
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int UserId { get; set; }
    public string Notes { get; set; } = null!;
    public int QuantityCount { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? DeletedAt { get; set; }  // soft delete timestamp
}
