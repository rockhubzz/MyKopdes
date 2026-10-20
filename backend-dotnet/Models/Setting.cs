namespace MyKopdes.Models;

// Maps to MySQL table `settings`
public class Setting
{
    public int Id { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("key")]
    public string Key { get; set; } = null!;
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string? Value { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("group")]
    public string? Group { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("is_public")]
    public bool IsPublic { get; set; } = false;
    [System.Text.Json.Serialization.JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
