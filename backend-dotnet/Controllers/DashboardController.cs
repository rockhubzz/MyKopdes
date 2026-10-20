using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Services;

namespace MyKopdes.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly KoperasiDbContext _context;

    public DashboardController(IReportService reportService, KoperasiDbContext context)
    {
        _reportService = reportService;
        _context = context;
    }

    [HttpGet("owner-summary")]
    [Authorize(Roles = "admin,owner,shop_owner")]
    public async Task<ActionResult<object>> GetOwnerSummary()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var endOfMonth = monthStart.AddMonths(1).AddDays(-1);

        // Aggregate in SQL: previously a full month of transactions *plus all
        // of their line items* was materialized and summed in C#. Here the DB
        // computes each transaction's item total and only two decimal columns
        // cross the wire — one row per transaction, not per line item.
        var todayStats = await _context.Transactions
            .Where(t => t.CreatedAt >= today && t.CreatedAt < today.AddDays(1))
            .Select(t => new
            {
                Total = t.TotalAmount,
                ItemTotal = (decimal?)t.Items.Sum(i => (i.Price ?? 0) * i.Quantity)
            })
            .ToListAsync();

        var monthStats = await _context.Transactions
            .Where(t => t.CreatedAt >= monthStart && t.CreatedAt <= endOfMonth)
            .Select(t => new
            {
                Total = t.TotalAmount,
                ItemTotal = (decimal?)t.Items.Sum(i => (i.Price ?? 0) * i.Quantity)
            })
            .ToListAsync();

        var todayRevenue = todayStats.Sum(t => (decimal)(t.Total ?? 0));
        var todayTransactions = todayStats.Count;
        var todayProfit = todayStats.Sum(t => (decimal)(t.Total ?? 0) - (t.ItemTotal ?? 0));

        var monthRevenue = monthStats.Sum(t => (decimal)(t.Total ?? 0));
        var monthTransactions = monthStats.Count;
        var monthProfit = monthStats.Sum(t => (decimal)(t.Total ?? 0) - (t.ItemTotal ?? 0));
        var monthProfitMargin = monthRevenue > 0 ? (monthProfit / monthRevenue * 100) : 0;

        var bestSellers = await _context.TransactionItems
            .AsNoTracking()
            .Where(ti => ti.CreatedAt >= monthStart && ti.CreatedAt <= endOfMonth)
            .GroupBy(ti => ti.Item != null ? ti.Item.Name : "Unknown")
            .Select(g => new
            {
                name = g.Key,
                units_sold = g.Sum(x => x.Quantity),
                revenue = g.Sum(x => (decimal)(x.Price ?? 0) * x.Quantity)
            })
            .OrderByDescending(x => x.revenue)
            .Take(5)
            .ToListAsync();

        var totalStockValueCost = await _context.Items.SumAsync(i => (i.Stock ?? 0) * (i.Cost ?? 0));
        var totalStockValueRetail = await _context.Items.SumAsync(i => (i.Stock ?? 0) * (i.Price ?? 0));
        var lowStockCount = await _context.Items
            .Where(i => (i.MinStock.HasValue && (i.Stock ?? 0) <= i.MinStock.Value) ||
                (!i.MinStock.HasValue && (i.Stock ?? 0) <= 10))
            .CountAsync();

        return Ok(new
        {
            today = new
            {
                revenue = todayRevenue,
                transaction_count = todayTransactions,
                profit = todayProfit
            },
            month_to_date = new
            {
                revenue = monthRevenue,
                transaction_count = monthTransactions,
                profit = monthProfit,
                profit_margin_pct = Math.Round(monthProfitMargin, 2)
            },
            best_sellers = bestSellers,
            stock = new
            {
                total_value_at_cost = totalStockValueCost,
                total_value_at_retail = totalStockValueRetail,
                low_stock_count = lowStockCount
            }
        });
    }

    [HttpGet("summary")]
    [Authorize(Roles = "admin,owner,manager")]
    public async Task<ActionResult<object>> GetSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var summary = await _reportService.GetDashboardSummaryAsync(from, to);
        return Ok(summary);
    }

    [HttpGet("sales-report")]
    [Authorize(Roles = "admin,owner,manager")]
    public async Task<ActionResult<object>> GetSalesReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var report = await _reportService.GetSalesReportAsync(from, to);
        return Ok(report);
    }

    [HttpGet("member-report/{memberId:int}")]
    [Authorize(Roles = "admin,owner,manager")]
    public async Task<ActionResult<object>> GetMemberReport(int memberId)
    {
        var report = await _reportService.GetMemberReportAsync(memberId);
        return Ok(report);
    }

    [HttpGet("stock-report")]
    [Authorize(Roles = "admin,owner,manager")]
    public async Task<ActionResult<object>> GetStockReport()
    {
        var report = await _reportService.GetStockReportAsync();
        return Ok(report);
    }

    [HttpGet("loan-report")]
    [Authorize(Roles = "admin,owner,manager")]
    public async Task<ActionResult<object>> GetLoanReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var report = await _reportService.GetLoanReportAsync(from, to);
        return Ok(report);
    }

    [HttpGet("alerts")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<object>> GetAlerts()
    {
        var lowStock = await _context.Items
            .Where(i => (i.MinStock.HasValue && (i.Stock ?? 0) <= i.MinStock.Value) ||
                (!i.MinStock.HasValue && (i.Stock ?? 0) <= 10))
            .Select(i => new
            {
                id = i.Id,
                name = i.Name,
                current_stock = i.Stock,
                min_stock_threshold = i.MinStock
            })
            .ToListAsync();

        var expiringSoon = await _context.Items
            .Where(i => i.ExpiryDate.HasValue && i.ExpiryDate <= DateTime.UtcNow.AddDays(7) && i.ExpiryDate > DateTime.UtcNow)
            .Select(i => new
            {
                id = i.Id,
                name = i.Name,
                expiry_date = i.ExpiryDate
            })
            .ToListAsync();

        return Ok(new
        {
            low_stock = lowStock,
            expiring_soon = expiringSoon
        });
    }

    [HttpGet("employee-summary")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<object>> GetEmployeeSummary()
    {
        var today = DateTime.UtcNow.Date;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var staffId = int.TryParse(userId, out var sid) ? sid : (int?)null;

        var transactionsToday = await _context.Transactions
            .Where(t => t.CreatedAt >= today && t.CreatedAt < today.AddDays(1))
            .CountAsync();

        var revenueToday = await _context.Transactions
            .Where(t => t.CreatedAt >= today && t.CreatedAt < today.AddDays(1))
            .SumAsync(t => (decimal)(t.TotalAmount ?? 0));

        var restocksToday = staffId.HasValue
            ? await _context.RestockingRecords
                .Where(r => r.CreatedAt >= today && r.CreatedAt < today.AddDays(1) && r.UserId == staffId)
                .CountAsync()
            : 0;

        return Ok(new
        {
            transactions_today = transactionsToday,
            revenue_today = revenueToday,
            restocks_submitted_today = restocksToday
        });
    }
}
