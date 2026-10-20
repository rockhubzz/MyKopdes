namespace MyKopdes.Models;

// Maps to MySQL table `item_categories`
public class ItemCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Inverse navigation for the items grouped in this category (not serialized
    // directly — the JSON contract exposes the count as `items_count`).
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<Item>? Items { get; set; }

    // Computed count of items in the category, populated by the service layer.
    [System.Text.Json.Serialization.JsonPropertyName("items_count")]
    public int ItemsCount { get; set; }
}
