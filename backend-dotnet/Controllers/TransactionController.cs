using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IAuditLogService _auditLogService;
    private readonly KoperasiDbContext _context;
    private readonly IMemoryCache _cache;

    public TransactionController(ITransactionService transactionService, IAuditLogService auditLogService, KoperasiDbContext context, IMemoryCache cache)
    {
        _transactionService = transactionService;
        _auditLogService = auditLogService;
        _context = context;
        _cache = cache;
    }

    /// <summary>List transactions, newest first, paginated. Search matches transaction code, member name, or cashier name.</summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Rows per page (per_page).</param>
    /// <param name="search">Optional text filter on code/member/cashier.</param>
    /// <param name="paymentMethod">Optional exact payment method filter (e.g. cash, qr).</param>
    [HttpGet]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<PaginatedResponse<Transaction>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? search = null,
        [FromQuery(Name = "payment_method")] string? paymentMethod = null)
    {
        var transactions = await _transactionService.GetAllAsync(page, pageSize, search, paymentMethod);
        var total = await _transactionService.GetTotalCountAsync(search, paymentMethod);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return Ok(new PaginatedResponse<Transaction>
        {
            Data = transactions,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages
        });
    }

    /// <summary>Fetch one transaction with its line items, member, and cashier.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Transaction>> GetById(int id)
    {
        var transaction = await _transactionService.GetByIdAsync(id);
        if (transaction == null) return NotFound(new { message = "Transaction not found" });
        return Ok(transaction);
    }

    /// <summary>
    /// Live price preview for the cashier: server-side discount and tax math
    /// without committing a sale. Body: items [{item_id, quantity}] plus an
    /// optional member_identifier and discount_id.
    /// </summary>
    [HttpPost("preview")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<object>> Preview([FromBody] CheckoutRequest request)
    {
        return Ok(await BuildCheckoutResult(request, commit: false));
    }

    /// <summary>
    /// Commit a sale: resolves the member and discount, applies tax,
    /// writes the transaction + line items in one DB transaction, deducts
    /// stock, and returns the receipt-ready transaction.
    /// </summary>
    [HttpPost("checkout")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Transaction>> Checkout([FromBody] CheckoutRequest request)
    {
        var result = await BuildCheckoutResult(request, commit: true);
        if (result.TransactionId == null) return BadRequest(new { message = "Checkout failed" });
        var transaction = await _transactionService.GetByIdAsync(result.TransactionId.Value);
        if (transaction == null) return NotFound(new { message = "Transaction not found" });
        return Ok(transaction);
    }

    [HttpPost]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Transaction>> Create([FromBody] Transaction transaction)
    {
        var created = await _transactionService.CreateAsync(transaction);
        return Created($"/api/transactions/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Transaction>> Update(int id, [FromBody] Transaction transaction)
    {
        try
        {
            var updated = await _transactionService.UpdateAsync(id, transaction);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Transaction not found" });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _transactionService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Transaction not found" });
        return Ok(new { message = "Transaction deleted successfully" });
    }

    [HttpGet("member/{memberId:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetByMember(int memberId)
    {
        var transactions = await _transactionService.GetByMemberAsync(memberId);
        return Ok(transactions);
    }

    [HttpPost("purchase")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Transaction>> CreatePurchase([FromBody] PurchaseRequest request)
    {
        var transaction = await _transactionService.ProcessPurchaseAsync(
            request.MemberId, request.Items, request.PaidAmount, request.Notes);
        return Created($"/api/transactions/{transaction.Id}", transaction);
    }

    [HttpPost("{id:int}/void")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<IActionResult> Void(int id)
    {
        var success = await _transactionService.VoidAsync(id);
        if (!success) return NotFound(new { message = "Transaction not found or cannot be voided" });

        // Log audit
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _auditLogService.CreateAsync(new AuditLog
        {
            UserId = int.TryParse(userId, out var uid) ? uid : null,
            UserType = "staff",
            Action = "void",
            EntityType = "transaction",
            EntityId = id
        });

        return Ok(new { message = "Transaction voided successfully" });
    }

    private async Task<CheckoutResult> BuildCheckoutResult(CheckoutRequest request, bool commit)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var staffId = int.TryParse(userId, out var sid) ? sid : (int?)null;

        // Load every cart line's item in ONE query. Previously this loop made
        // one FindAsync per line — and the commit path re-queried each item a
        // second time — so a 10-item sale issued 20+ sequential DB round trips.
        var itemIds = request.Items.Select(l => l.ItemId).Distinct().ToList();
        var itemsById = itemIds.Count == 0
            ? new Dictionary<int, Models.Item>()
            : await _context.Items
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

        var lines = new List<dynamic>();
        decimal subtotal = 0;

        foreach (var line in request.Items)
        {
            if (!itemsById.TryGetValue(line.ItemId, out var item)) continue;

            var unitPrice = (decimal)(item.Price ?? 0);
            var qty = line.Quantity;
            var lineSubtotal = unitPrice * qty;
            subtotal += lineSubtotal;

            lines.Add(new
            {
                item_id = item.Id,
                name = item.Name,
                quantity = qty,
                unit_price = unitPrice,
                subtotal = lineSubtotal
            });
        }

        // Resolve member
        Models.Member? member = null;
        if (!string.IsNullOrEmpty(request.MemberIdentifier))
        {
            member = await _context.Members
                .FirstOrDefaultAsync(m => m.Identifier == request.MemberIdentifier ||
                    m.Phone == request.MemberIdentifier ||
                    m.Email == request.MemberIdentifier);
        }

        // Resolve discount — fetch only the rows that can actually apply,
        // instead of loading the entire active-discounts table.
        Models.Discount? discount = null;
        decimal discountAmount = 0;
        if (request.DiscountId.HasValue)
        {
            discount = await _context.Discounts
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.Id == request.DiscountId.Value && d.IsActive);
        }
        else if (member != null)
        {
            discount = await _context.Discounts
                .Include(d => d.Category)
                .Where(d => d.IsActive && d.Scope == "member")
                .OrderBy(d => d.Id)
                .FirstOrDefaultAsync();
        }

        if (discount != null)
        {
            if (discount.Type == "percentage")
            {
                discountAmount = Math.Round(subtotal * (decimal)(discount.DiscountRate) / 100, 2);
            }
            else if (discount.Type == "flat")
            {
                discountAmount = (decimal)(discount.DiscountRate);
            }
            // buy_x_get_y — simplified: skip for preview, apply in checkout
        }

        // Tax rate from settings — cached briefly (queries hit this row on
        // every preview keystroke debounce and checkout otherwise).
        var taxRate = await GetTaxRateAsync();
        var taxableAmount = subtotal - discountAmount;
        var taxAmount = Math.Round(taxableAmount * taxRate, 2);
        var total = subtotal - discountAmount + taxAmount;

        if (commit)
        {
            // Concurrency safety: GenerateTransactionNumberAsync is count-based,
            // so two simultaneous checkouts can compute the same TRX- code.
            // Serialize allocation with a MySQL advisory lock held across
            // number generation + insert, with a bounded retry as backstop
            // (transaction_code has a UNIQUE index that would otherwise 500).
            const int maxAttempts = 4;
            for (var attempt = 1; ; attempt++)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                var lockAcquired = await AcquireTrxNumberLockAsync();

                try
                {
                    var txnNumber = await _transactionService.GenerateTransactionNumberAsync();
                    var dbTransaction = new Transaction
                    {
                        TransactionNumber = txnNumber,
                        MemberId = member?.Id,
                        UserId = staffId,
                        Type = "purchase",
                        Subtotal = subtotal,
                        DiscountAmount = discountAmount,
                        TaxAmount = taxAmount,
                        TotalAmount = total,
                        PaymentMethod = request.PaymentMethod ?? "cash",
                        PaymentStatus = "paid",
                        PaidAt = DateTime.UtcNow,
                        Notes = request.Notes,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Transactions.Add(dbTransaction);
                    await _context.SaveChangesAsync();

                    foreach (var line in request.Items)
                    {
                        if (!itemsById.TryGetValue(line.ItemId, out var item)) continue;

                        var unitPrice = (decimal)(item.Price ?? 0);
                        _context.TransactionItems.Add(new TransactionItem
                        {
                            TransactionId = dbTransaction.Id,
                            ItemId = item.Id,
                            Quantity = line.Quantity,
                            Price = unitPrice,
                            Subtotal = unitPrice * line.Quantity,
                            CreatedAt = DateTime.UtcNow
                        });

                        // itemsById holds tracked entities, so this is recorded.
                        item.Stock -= line.Quantity;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new CheckoutResult
                    {
                        TransactionId = dbTransaction.Id,
                        TransactionCode = dbTransaction.TransactionNumber,
                        Total = total
                    };
                }
                catch (DbUpdateException ex) when (attempt < maxAttempts && IsDuplicateKey(ex))
                {
                    // Sweep the failed attempt away, back off briefly (lets the
                    // winner's count commit), then retry with a fresh number.
                    foreach (var entry in _context.ChangeTracker.Entries().ToList())
                        entry.State = EntityState.Detached;
                    await Task.Delay(Random.Shared.Next(25, 75));
                }
                finally
                {
                    if (lockAcquired) await ReleaseTrxNumberLockAsync();
                }
            }
        }

        var warnings = new List<string>();
        if (discount != null && discount.Scope == "member" && member == null)
        {
            warnings.Add("A member discount is available — add a member to apply it.");
        }

        var discountObj = discount != null ? new { name = discount.Name, type = discount.Type, value = discount.DiscountRate } : null;
        var memberObj = member != null ? new { id = member.Id, name = member.Name, membership_id = member.Identifier } : null;

        return new CheckoutResult
        {
            Lines = lines,
            Subtotal = subtotal,
            Discount = discountObj,
            DiscountAmount = discountAmount,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            Total = total,
            Member = memberObj,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Tax rate (e.g. 0.11 for 11%) from the "tax_rate" setting, memoized in
    /// a short in-memory cache so preview/checkout don't query the settings
    /// table on every keystroke-debounce. Invalidated when the setting is
    /// updated (see SettingsService.UpdateOrCreateAsync); TTL bounds the rest.
    /// </summary>
    private Task<decimal> GetTaxRateAsync()
        => _cache.GetOrCreateAsync("tax_rate", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return LoadTaxRateFromDatabaseAsync();
        })!;

    private async Task<decimal> LoadTaxRateFromDatabaseAsync()
    {
        var value = await _context.Settings
            .AsNoTracking()
            .Where(s => s.Key == "tax_rate")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        return decimal.TryParse(value ?? "0", out var rate) ? rate / 100 : 0;
    }

    private static bool IsDuplicateKey(DbUpdateException ex)
        => ex.InnerException is MySqlConnector.MySqlException { Number: 1062 };

    /// <summary>
    /// MySQL advisory lock serializing transaction-number allocation across
    /// concurrent checkouts. Held on this request's dedicated connection.
    /// </summary>
    private async Task<bool> AcquireTrxNumberLockAsync()
    {
        var got = await _context.Database
            .SqlQueryRaw<long?>("SELECT GET_LOCK('koperasi_trx_number', 5)")
            .ToListAsync();
        return got.FirstOrDefault() == 1;
    }

    private async Task ReleaseTrxNumberLockAsync()
    {
        // RELEASE_LOCK can legally return NULL (lock not held) — hence long?.
        await _context.Database
            .SqlQueryRaw<long?>("SELECT RELEASE_LOCK('koperasi_trx_number')")
            .ToListAsync();
    }
}

public class PurchaseRequest
{
    public int MemberId { get; set; }
    public List<TransactionItem> Items { get; set; } = new();
    public decimal PaidAmount { get; set; }
    public string? Notes { get; set; }
}

public class CheckoutRequest
{
    public List<CheckoutItem> Items { get; set; } = new();
    public string? MemberIdentifier { get; set; }
    public int? DiscountId { get; set; }
    public string? PaymentMethod { get; set; } = "cash";
    public string? Notes { get; set; }
}

public class CheckoutItem
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class CheckoutResult
{
    public int? TransactionId { get; set; }
    public string? TransactionCode { get; set; }
    public decimal? Total { get; set; }
    public List<dynamic>? Lines { get; set; }
    public decimal? Subtotal { get; set; }
    public object? Discount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public object? Member { get; set; }
    public List<string>? Warnings { get; set; }
}
