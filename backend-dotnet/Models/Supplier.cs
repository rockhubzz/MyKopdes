using MyKopdes.Models.DTOs;

namespace MyKopdes.Models;

// Maps to MySQL table `suppliers`
public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<RestockingRecord>? RestockingRecords { get; set; }

    // The detail drawer uses the camelCase relation name used by the
    // frontend. Keep the EF navigation ignored so list responses stay small.
    [System.Text.Json.Serialization.JsonPropertyName("restockingRecords")]
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public IEnumerable<RestockingRecordResponse>? RestockingRecordsForApi =>
        RestockingRecords?.Select(RestockingRecordResponse.From);
}
