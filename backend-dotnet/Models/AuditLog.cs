using System.Text.Json.Serialization;

namespace MyKopdes.Models;

// Maps to MySQL table `audit_logs`
public class AuditLog
{
    public int Id { get; set; }

    [JsonPropertyName("actor_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("actor_type")]
    public string? UserType { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("auditable_type")]
    public string? EntityType { get; set; }

    [JsonPropertyName("auditable_id")]
    public int? EntityId { get; set; }

    [JsonPropertyName("changes")]
    public string? Details { get; set; }

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    // Navigation property (serializes as "actor")
    [JsonPropertyName("actor")]
    public User? User { get; set; }
}
