using System.Text.Json.Serialization;
using MyKopdes.Models;

namespace MyKopdes.Models.DTOs;

public sealed class MemberTransactionSummary
{
    public int Id { get; init; }
    [JsonPropertyName("transaction_code")]
    public string TransactionCode { get; init; } = null!;
    public decimal? Total { get; init; }
    [JsonPropertyName("payment_status")]
    public string? PaymentStatus { get; init; }
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; init; }

    public static MemberTransactionSummary From(Transaction transaction) => new()
    {
        Id = transaction.Id,
        TransactionCode = transaction.TransactionNumber,
        Total = transaction.TotalAmount,
        PaymentStatus = transaction.PaymentStatus,
        CreatedAt = transaction.CreatedAt
    };
}
