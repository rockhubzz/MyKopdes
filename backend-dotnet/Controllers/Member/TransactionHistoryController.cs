using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Member;

[ApiController]
[Route("api/member")]
public class TransactionHistoryController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly KoperasiDbContext _context;

    public TransactionHistoryController(ITransactionService transactionService, KoperasiDbContext context)
    {
        _transactionService = transactionService;
        _context = context;
    }

    [HttpGet("transactions")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<PaginatedResponse<Transaction>>> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var query = _context.Transactions
            .AsNoTracking()
            .AsSplitQuery()
            .Where(t => t.MemberId == userId.Value)
            .Include(t => t.User)
            .Include(t => t.Discount)
            .ThenInclude(d => d!.Category)
            .Include(t => t.Items)
            .ThenInclude(ti => ti.Item);

        var total = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PaginatedResponse<Transaction>
        {
            Data = transactions,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    [HttpGet("transactions/{id:int}")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<Transaction>> GetTransaction(int id)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var transaction = await _transactionService.GetByIdAsync(id);
        if (transaction == null) return NotFound(new { message = "Transaction not found" });
        if (transaction.MemberId != userId.Value) return Forbid();
        return Ok(transaction);
    }
}
