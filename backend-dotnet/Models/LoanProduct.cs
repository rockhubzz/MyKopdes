namespace MyKopdes.Models;

// Maps to MySQL table `loan_products`
public class LoanProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal? InterestRate { get; set; }    // interest_rate
    public string InterestPeriod { get; set; } = "monthly";
    public int? MinTermMonths { get; set; } = 1;
    public int? MaxTermMonths { get; set; } = 12;
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? ProcessingFeeRate { get; set; }
    public string? EligibilityRule { get; set; }
    public bool? IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
