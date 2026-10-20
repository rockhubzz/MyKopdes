using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class ReportService : IReportService
{
    private readonly KoperasiDbContext _context;

    public ReportService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<string, object>> GetDashboardSummaryAsync(DateTime? from = null, DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddDays(-30);
        to ??= DateTime.UtcNow;

        // One grouped query replaces four separate COUNT scans over loans.
        var loanStats = await _context.Loans
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(l => l.Status == "active" || l.Status == "approved"),
                Pending = g.Count(l => l.Status == "pending")
            })
            .FirstOrDefaultAsync();
        var (totalLoans, activeLoans, pendingLoans) = loanStats is null
            ? (0, 0, 0)
            : (loanStats.Total, loanStats.Active, loanStats.Pending);

        var totalMembers = await _context.Members.CountAsync();
        var totalItems = await _context.Items.CountAsync();

        var totalSales = await _context.Transactions
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to)
            .SumAsync(t => (decimal)(t.TotalAmount ?? 0));

        var totalLoansAmount = await _context.Loans
            .Where(l => l.CreatedAt >= from && l.CreatedAt <= to)
            .SumAsync(l => (decimal)(l.PrincipalAmount ?? 0));

        var lowStockItems = await _context.Items
            .Where(i => (i.MinStock.HasValue && (i.Stock ?? 0) <= i.MinStock.Value) ||
                (!i.MinStock.HasValue && (i.Stock ?? 0) <= 10))
            .CountAsync();

        var recentTransactions = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Member)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new
            {
                TransactionNumber = t.TransactionNumber,
                MemberName = t.Member != null ? t.Member.Name : null,
                Type = t.Type,
                TotalAmount = t.TotalAmount,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return new Dictionary<string, object>
        {
            ["total_members"] = totalMembers,
            ["total_items"] = totalItems,
            ["total_loans"] = totalLoans,
            ["active_loans"] = activeLoans,
            ["pending_loans"] = pendingLoans,
            ["total_sales"] = totalSales,
            ["total_loans_amount"] = totalLoansAmount,
            ["low_stock_items"] = lowStockItems,
            ["recent_transactions"] = recentTransactions
        };
    }

    public async Task<Dictionary<string, object>> GetSalesReportAsync(DateTime from, DateTime to)
    {
        var transactions = await _context.Transactions
            .Include(t => t.Member)
            .Include(t => t.User)
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to)
            .Select(t => new
            {
                TransactionNumber = t.TransactionNumber,
                MemberName = t.Member != null ? t.Member.Name : null,
                TotalAmount = t.TotalAmount,
                PaidAmount = t.PaidAmount,
                DiscountAmount = t.DiscountAmount,
                TaxAmount = t.TaxAmount,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        var totalSales = transactions.Sum(t => (decimal)(t.TotalAmount ?? 0));
        var totalPaid = transactions.Sum(t => (decimal)(t.PaidAmount ?? 0));
        var totalDiscount = transactions.Sum(t => (decimal)(t.DiscountAmount ?? 0));
        var totalTax = transactions.Sum(t => (decimal)(t.TaxAmount ?? 0));

        return new Dictionary<string, object>
        {
            ["total_sales"] = totalSales,
            ["total_paid"] = totalPaid,
            ["total_discount"] = totalDiscount,
            ["total_tax"] = totalTax,
            ["transaction_count"] = transactions.Count,
            ["transactions"] = transactions
        };
    }

    public async Task<Dictionary<string, object>> GetMemberReportAsync(int memberId)
    {
        var member = await _context.Members.FindAsync(memberId);
        if (member == null) return new Dictionary<string, object>();

        var transactions = await _context.Transactions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Items)
            .ThenInclude(ti => ti.Item)
            .Where(t => t.MemberId == memberId)
            .ToListAsync();

        var loans = await _context.Loans
            .AsNoTracking()
            .AsSplitQuery()
            .Include(l => l.LoanProduct)
            .Include(l => l.Payments)
            .Where(l => l.MemberId == memberId)
            .ToListAsync();

        return new Dictionary<string, object>
        {
            ["member"] = member,
            ["transaction_count"] = transactions.Count,
            ["total_spent"] = transactions.Sum(t => (decimal)(t.TotalAmount ?? 0)),
            ["total_paid"] = transactions.Sum(t => (decimal)(t.PaidAmount ?? 0)),
            ["loan_count"] = loans.Count,
            ["total_loan_principal"] = loans.Sum(l => (decimal)(l.PrincipalAmount ?? 0)),
            ["total_loan_paid"] = loans.Sum(l => (decimal)(l.PaidAmount ?? 0)),
            ["remaining_loan_balance"] = loans.Sum(l => (decimal)(l.RemainingAmount ?? 0)),
            ["transactions"] = transactions,
            ["loans"] = loans
        };
    }

    public async Task<Dictionary<string, object>> GetStockReportAsync()
    {
        var items = await _context.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Code,
                Category = i.Category != null ? i.Category.Name : null,
                Price = i.Price,
                Cost = i.Cost,
                Stock = i.Stock,
                i.MinStock,
                StockValue = (i.Stock ?? 0) * (i.Cost ?? 0)
            })
            .ToListAsync();

        var totalStockValue = items.Sum(i => (decimal?)i.StockValue ?? 0);

        return new Dictionary<string, object>
        {
            ["total_items"] = items.Count,
            ["total_stock_value"] = totalStockValue,
            ["low_stock_items"] = items.Where(i => (i.Stock ?? 0) <= (i.MinStock ?? 10)).ToList(),
            ["items"] = items
        };
    }

    public async Task<Dictionary<string, object>> GetLoanReportAsync(DateTime from, DateTime to)
    {
        var loans = await _context.Loans
            .AsNoTracking()
            .AsSplitQuery()
            .Include(l => l.Member)
            .Include(l => l.LoanProduct)
            .Include(l => l.Payments)
            .Where(l => l.CreatedAt >= from && l.CreatedAt <= to)
            .ToListAsync();

        return new Dictionary<string, object>
        {
            ["total_loans"] = loans.Count,
            ["total_principal"] = loans.Sum(l => (decimal)(l.PrincipalAmount ?? 0)),
            ["total_interest"] = loans.Sum(l => (decimal)(l.InterestAmount ?? 0)),
            ["total_paid"] = loans.Sum(l => (decimal)(l.PaidAmount ?? 0)),
            ["total_remaining"] = loans.Sum(l => (decimal)(l.RemainingAmount ?? 0)),
            ["by_status"] = loans.GroupBy(l => l.Status)
                .ToDictionary(g => g.Key, g => g.Count()),
            ["loans"] = loans
        };
    }
}
