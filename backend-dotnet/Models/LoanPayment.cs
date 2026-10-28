namespace MyKopdes.Models;

// Maps to MySQL table `loan_payments`
public class LoanPayment
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Loan Loan { get; set; } = null!;
    [System.Text.Json.Serialization.JsonPropertyName("recorded_by")]
    public int? UserId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public User? User { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("recordedBy")]
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public User? RecordedBy => User;
    public decimal? Amount { get; set; }
    public decimal? InterestAmount { get; set; } = 0;
    public decimal? PrincipalAmount { get; set; } = 0;
    [System.Text.Json.Serialization.JsonPropertyName("paid_at")]
    public DateTime PaymentDate { get; set; }        // paid_at
    public string? Notes { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
