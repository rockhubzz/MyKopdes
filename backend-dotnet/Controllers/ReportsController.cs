using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly KoperasiDbContext _context;

    public ReportsController(KoperasiDbContext context)
    {
        _context = context;
    }

    [HttpGet("stock-valuation")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<object>> GetStockValuation()
    {
        var totalValueAtCost = await _context.Items.SumAsync(i => (i.Stock ?? 0) * (i.Cost ?? 0));
        var totalValueAtRetail = await _context.Items.SumAsync(i => (i.Stock ?? 0) * (i.Price ?? 0));
        var lowStockCount = await _context.Items
            .Where(i => (i.MinStock.HasValue && (i.Stock ?? 0) <= i.MinStock.Value) ||
                (!i.MinStock.HasValue && (i.Stock ?? 0) <= 10))
            .CountAsync();
        var potentialProfit = totalValueAtRetail - totalValueAtCost;

        return Ok(new
        {
            total_value_at_cost = totalValueAtCost,
            total_value_at_retail = totalValueAtRetail,
            potential_profit_if_all_sold = potentialProfit,
            low_stock_count = lowStockCount
        });
    }

    [HttpGet("summary")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<object>> GetSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        // The page sends date-only values (YYYY-MM-DD); treat `to` as an
        // inclusive day so rows from that whole day are counted.
        var toExclusive = (to ?? DateTime.UtcNow).Date.AddDays(1);

        var revenue = await _context.Transactions
            .Where(t => t.CreatedAt >= from && t.CreatedAt < toExclusive)
            .SumAsync(t => (decimal)(t.TotalAmount ?? 0));

        var cost = await _context.Transactions
            .Where(t => t.CreatedAt >= from && t.CreatedAt < toExclusive)
            .SelectMany(t => t.Items!)
            .SumAsync(ti => (ti.Item != null ? (ti.Item.Cost ?? 0) : 0m) * ti.Quantity);

        var transactionCount = await _context.Transactions
            .Where(t => t.CreatedAt >= from && t.CreatedAt < toExclusive)
            .CountAsync();

        var discountGiven = await _context.Transactions
            .Where(t => t.CreatedAt >= from && t.CreatedAt < toExclusive)
            .SumAsync(t => (decimal)(t.DiscountAmount ?? 0));

        var taxCollected = await _context.Transactions
            .Where(t => t.CreatedAt >= from && t.CreatedAt < toExclusive)
            .SumAsync(t => (decimal)(t.TaxAmount ?? 0));

        var profit = revenue - cost;
        var profitMargin = revenue > 0 ? (profit / revenue * 100) : 0;
        var averageBasket = transactionCount > 0 ? (revenue / transactionCount) : 0;

        return Ok(new
        {
            revenue,
            cost_of_goods_sold = cost,
            profit = profit,
            profit_margin_pct = Math.Round(profitMargin, 2),
            discount_given = discountGiven,
            tax_collected = taxCollected,
            transaction_count = transactionCount,
            average_basket = Math.Round(averageBasket, 2)
        });
    }

    [HttpGet("best-sellers")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<IEnumerable<object>>> GetBestSellers([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        var toExclusive = (to ?? DateTime.UtcNow).Date.AddDays(1);

        var bestSellers = await _context.TransactionItems
            .Include(ti => ti.Item)
            .Where(ti => ti.CreatedAt >= from && ti.CreatedAt < toExclusive)
            .GroupBy(ti => ti.Item != null ? ti.Item.Code : "unknown")
            .Select(g => new
            {
                name = g.FirstOrDefault()!.Item!.Name,
                sku = g.Key,
                units_sold = g.Sum(x => x.Quantity),
                revenue = g.Sum(x => (decimal)(x.Price ?? 0) * x.Quantity)
            })
            .OrderByDescending(x => x.revenue)
            .Take(10)
            .ToListAsync();

        return Ok(bestSellers);
    }

    [HttpGet("transactions/export")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<IActionResult> ExportTransactions([FromQuery] string format = "csv", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        var toExclusive = (to ?? DateTime.UtcNow).Date.AddDays(1);

        var transactions = await _context.Transactions
            .Include(t => t.Member)
            .Include(t => t.User)
            .Include(t => t.Items)
            .ThenInclude(ti => ti.Item)
            .Where(t => t.CreatedAt >= from && t.CreatedAt < toExclusive)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        if (format == "csv")
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("transaction_code,member_name,cashier,total,total_items,created_at");
            foreach (var t in transactions)
            {
                var memberName = t.Member?.Name ?? "-";
                var cashierName = t.User?.Name ?? "-";
                var itemCount = t.Items?.Count ?? 0;
                csv.AppendLine($"{t.TransactionNumber},{memberName},{cashierName},{t.TotalAmount},{itemCount},{t.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"transactions_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.csv");
        }

        // For xlsx and pdf — fallback to CSV if the libraries aren't available
        var csv2 = new System.Text.StringBuilder();
        csv2.AppendLine("transaction_code,member_name,cashier,total,total_items,created_at");
        foreach (var t in transactions)
        {
            var memberName = t.Member?.Name ?? "-";
            var cashierName = t.User?.Name ?? "-";
            var itemCount = t.Items?.Count ?? 0;
            csv2.AppendLine($"{t.TransactionNumber},{memberName},{cashierName},{t.TotalAmount},{itemCount},{t.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        var bytes2 = System.Text.Encoding.UTF8.GetBytes(csv2.ToString());
        var contentType = format == "xlsx" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf";
        return File(bytes2, contentType, $"transactions_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.{format}");
    }

    [HttpGet("loan-report")]
    [Authorize(Roles = "admin,shop_owner,manager")]
    public async Task<ActionResult<object>> GetLoanReport([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        var toExclusive = (to ?? DateTime.UtcNow).Date.AddDays(1);
        var now = DateTime.UtcNow;

        var loans = await _context.Loans
            .AsNoTracking()
            .Include(l => l.Member)
            .Include(l => l.LoanProduct)
            .Where(l => l.CreatedAt >= from && l.CreatedAt < toExclusive)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        // RemainingAmount is EF-ignored (computed, not a DB column), so it is
        // derived here rather than trusting the property.
        decimal outstandingOf(Loan l) =>
            Math.Max((l.TotalAmount ?? ((l.PrincipalAmount ?? 0) + (l.InterestAmount ?? 0))) - (l.PaidAmount ?? 0), 0);

        var byStatus = loans
            .GroupBy(l => l.Status ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        return Ok(new
        {
            total_loans = loans.Count,
            total_principal = loans.Sum(l => l.PrincipalAmount ?? 0),
            total_interest = loans.Sum(l => l.InterestAmount ?? 0),
            total_paid = loans.Sum(l => l.PaidAmount ?? 0),
            total_outstanding = loans.Sum(outstandingOf),
            overdue_count = loans.Count(l => l.Status == "active" && l.DueDate.HasValue && l.DueDate.Value < now),
            active_count = loans.Count(l => l.Status == "active"),
            pending_count = loans.Count(l => l.Status == "pending"),
            paid_off_count = loans.Count(l => l.Status == "paid_off"),
            by_status = byStatus,
            loans = loans.Select(l => new
            {
                id = l.Id,
                loan_code = l.LoanCode,
                member_id = l.MemberId,
                member_name = l.Member != null ? l.Member.Name : null,
                membership_id = l.Member != null ? l.Member.Identifier : null,
                product_name = l.LoanProduct != null ? l.LoanProduct.Name : null,
                principal = l.PrincipalAmount ?? 0,
                accrued_interest = l.InterestAmount ?? 0,
                interest_rate = l.InterestRate,
                term_months = l.TermInMonths,
                total_amount = l.TotalAmount ?? ((l.PrincipalAmount ?? 0) + (l.InterestAmount ?? 0)),
                total_paid = l.PaidAmount ?? 0,
                remaining_amount = outstandingOf(l),
                status = l.Status,
                applied_at = l.LoanDate,
                due_at = l.DueDate,
                paid_off_at = l.PaidAt
            })
        });
    }
}
