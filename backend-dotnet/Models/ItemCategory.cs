namespace MyKopdes.Models;

// Maps to MySQL table `item_categories`
public class ItemCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
