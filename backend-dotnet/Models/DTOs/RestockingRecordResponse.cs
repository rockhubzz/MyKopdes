using System.Text.Json.Serialization;
using MyKopdes.Models;

namespace MyKopdes.Models.DTOs;

// Flat response wrapper prevents the Item/Supplier EF graph from recursively
// serializing its inverse restocking-record navigation.
public sealed class RestockingRecordResponse
{
    public int Id { get; init; }
    public int ItemId { get; init; }
    public Item? Item { get; init; }
    public int? SupplierId { get; init; }
    public SupplierSummary? Supplier { get; init; }
    [JsonPropertyName("submitted_by")]
    public int? SubmittedBy { get; init; }
    [JsonPropertyName("submittedBy")]
    public User? SubmittedByUser { get; init; }
    public int Quantity { get; init; }
    public decimal? CostPerUnit { get; init; }
    public decimal? TotalCost { get; init; }
    public string? Notes { get; init; }
    [JsonPropertyName("restocked_at")]
    public DateTime? RestockedAt { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static RestockingRecordResponse From(RestockingRecord record) => new()
    {
        Id = record.Id,
        ItemId = record.ItemId,
        Item = record.Item,
        SupplierId = record.SupplierId,
        Supplier = record.Supplier == null ? null : SupplierSummary.From(record.Supplier),
        SubmittedBy = record.UserId,
        SubmittedByUser = record.User,
        Quantity = record.Quantity,
        CostPerUnit = record.CostPerUnit,
        TotalCost = record.TotalCost,
        Notes = record.Notes,
        RestockedAt = record.RestockDate,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt
    };
}

public sealed class SupplierSummary
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    [JsonPropertyName("contact_person")]
    public string? ContactPerson { get; init; }

    public static SupplierSummary From(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        Phone = supplier.Phone,
        Email = supplier.Email,
        Address = supplier.Address,
        ContactPerson = supplier.ContactPerson
    };
}
