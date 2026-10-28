using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;
using MyKopdes.Middleware;

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
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<PaginatedResponse<Transaction>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? search = null,
        [FromQuery(Name = "payment_method")] string? paymentMethod = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery(Name = "member_id")] int? memberId = null)
    {
        var cashierId = User.IsInRole("employee") ? User.GetUserId() : null;
        var transactions = await _transactionService.GetAllAsync(page, Math.Min(pageSize, 200), search, paymentMethod, from, to, memberId, cashierId);
        var total = await _transactionService.GetTotalCountAsync(search, paymentMethod, from, to, memberId, cashierId);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return Ok(new PaginatedResponse<Transaction>
        {
            Data = transactions,
            Page = page,
            PageSize = Math.Min(pageSize, 200),
            Total = total,
            TotalPages = totalPages
        });
    }

    /// <summary>Fetch one transaction with its line items, member, and cashier.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
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
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<object>> Preview([FromBody] CheckoutRequest request)
    {
        var validationError = ValidateCheckoutRequest(request, requirePaymentMethod: false);
        if (validationError != null) return BadRequest(new { message = validationError });

        try
        {
            return Ok(await BuildCheckoutResult(request, commit: false));
        }
        catch (CheckoutValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Commit a sale: resolves the member and discount, applies tax,
    /// writes the transaction + line items in one DB transaction, deducts
    /// stock, and returns the receipt-ready transaction.
    /// </summary>
    [HttpPost("checkout")]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<Transaction>> Checkout([FromBody] CheckoutRequest request)
    {
        var validationError = ValidateCheckoutRequest(request, requirePaymentMethod: true);
        if (validationError != null) return BadRequest(new { message = validationError });

        CheckoutResult result;
        try
        {
            result = await BuildCheckoutResult(request, commit: true);
        }
        catch (CheckoutValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        if (result.TransactionId == null) return BadRequest(new { message = "Checkout failed" });
        var transaction = await _transactionService.GetByIdAsync(result.TransactionId.Value);
        if (transaction == null) return NotFound(new { message = "Transaction not found" });
        return Ok(transaction);
    }

    [HttpPost]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<Transaction>> Create([FromBody] Transaction transaction)
    {
        var created = await _transactionService.CreateAsync(transaction);
        return Created($"/api/transactions/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
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
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _transactionService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Transaction not found" });
        return Ok(new { message = "Transaction deleted successfully" });
    }

    [HttpGet("member/{memberId:int}")]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetByMember(int memberId)
    {
        var transactions = await _transactionService.GetByMemberAsync(memberId);
        return Ok(transactions);
    }

    [HttpPost("purchase")]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<Transaction>> CreatePurchase([FromBody] PurchaseRequest request)
    {
        var transaction = await _transactionService.ProcessPurchaseAsync(
            request.MemberId, request.Items, request.PaidAmount, request.Notes);
        return Created($"/api/transactions/{transaction.Id}", transaction);
    }

    [HttpPost("{id:int}/void")]
    [Authorize(Roles = "admin,shop_owner")]
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

    [HttpGet("{id:int}/receipt")]
    [Authorize(Roles = "admin,shop_owner,employee")]
    public async Task<ActionResult<object>> Receipt(int id)
    {
        var transaction = await _transactionService.GetByIdAsync(id);
        if (transaction == null) return NotFound(new { message = "Transaction not found" });

        var settings = await _context.Settings
            .AsNoTracking()
            .Where(s => s.Key == "store_name" || s.Key == "store_address")
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? "");

        return Ok(new
        {
            transaction,
            store = new
            {
                name = settings.GetValueOrDefault("store_name", "Koperasi"),
                address = settings.GetValueOrDefault("store_address", "")
            }
        });
    }

    private async Task<CheckoutResult> BuildCheckoutResult(CheckoutRequest request, bool commit)
    {
        var staffId = User.GetUserId();
        var itemIds = request.Items.Select(l => l.ItemId).Distinct().ToList();
        var itemsById = await _context.Items
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id);

        var lines = new List<CheckoutLine>();
        decimal subtotal = 0;
        var warnings = new List<string>();

        foreach (var line in request.Items)
        {
            if (!itemsById.TryGetValue(line.ItemId, out var item) || !item.IsActive)
                throw new CheckoutValidationException($"Item #{line.ItemId} is not available.");

            if ((item.Stock ?? 0) < line.Quantity)
                warnings.Add($"Only {item.Stock ?? 0} × \"{item.Name}\" in stock.");

            var unitPrice = item.Price ?? 0;
            var lineSubtotal = Math.Round(unitPrice * line.Quantity, 2);
            subtotal += lineSubtotal;
            lines.Add(new CheckoutLine
            {
                ItemId = item.Id,
                Name = item.Name,
                CategoryId = item.CategoryId,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                Subtotal = lineSubtotal
            });
        }
        subtotal = Math.Round(subtotal, 2);

        Models.Member? member = null;
        if (!string.IsNullOrWhiteSpace(request.MemberIdentifier))
        {
            var identifier = request.MemberIdentifier.Trim();
            member = await _context.Members.FirstOrDefaultAsync(m =>
                m.Identifier == identifier || m.Phone == identifier || m.Email == identifier);
            if (member == null || !member.IsActive)
                throw new CheckoutValidationException("No active member matches that ID, email, or phone.");
        }

        var now = DateTime.UtcNow;
        var discounts = await _context.Discounts
            .Include(d => d.Category)
            .Where(d => d.IsActive &&
                (d.StartDate == null || d.StartDate <= now) &&
                (d.EndDate == null || d.EndDate >= now) &&
                (d.Scope != "member" || member != null))
            .Where(d => !request.DiscountId.HasValue || d.Id == request.DiscountId.Value)
            .ToListAsync();

        Models.Discount? discount = null;
        decimal discountAmount = 0;
        foreach (var candidate in discounts)
        {
            if (candidate.MinPurchase.HasValue && subtotal < candidate.MinPurchase.Value) continue;
            var candidateAmount = ComputeDiscountAmount(candidate, lines, subtotal);
            if (candidateAmount > discountAmount)
            {
                discount = candidate;
                discountAmount = candidateAmount;
            }
        }
        discountAmount = Math.Min(Math.Round(discountAmount, 2), subtotal);

        if (member == null && !request.DiscountId.HasValue &&
            await _context.Discounts.AnyAsync(d => d.IsActive && d.Scope == "member" &&
                (d.StartDate == null || d.StartDate <= now) && (d.EndDate == null || d.EndDate >= now)))
            warnings.Add("A member discount is available — add a member to apply it.");

        var taxRate = await GetTaxRateAsync();
        var taxableAmount = Math.Max(subtotal - discountAmount, 0);
        var taxAmount = Math.Round(taxableAmount * taxRate, 2);
        var total = Math.Round(taxableAmount + taxAmount, 2);

        if (commit)
        {
            const int maxAttempts = 6;
            for (var attempt = 1; ; attempt++)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var lockAcquired = await AcquireTrxNumberLockAsync();

                try
                {
                    var txnNumber = await _transactionService.GenerateTransactionNumberAsync();
                    var dbTransaction = new Transaction
                    {
                        TransactionNumber = txnNumber,
                        MemberId = member?.Id,
                        UserId = staffId,
                        DiscountId = discount?.Id,
                        Type = "purchase",
                        Subtotal = subtotal,
                        DiscountAmount = discountAmount,
                        TaxAmount = taxAmount,
                        TotalAmount = total,
                        PaymentMethod = request.PaymentMethod,
                        PaymentStatus = "paid",
                        PaidAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Transactions.Add(dbTransaction);
                    await _context.SaveChangesAsync();

                    foreach (var line in lines)
                    {
                        _context.TransactionItems.Add(new TransactionItem
                        {
                            TransactionId = dbTransaction.Id,
                            ItemId = line.ItemId,
                            Quantity = line.Quantity,
                            Price = line.UnitPrice,
                            Subtotal = line.Subtotal,
                            CreatedAt = DateTime.UtcNow
                        });

                        // The stock predicate makes the decrement safe when
                        // two cashiers sell the last units at the same time.
                        var changed = await _context.Items
                            .Where(i => i.Id == line.ItemId && i.IsActive && (i.Stock ?? 0) >= line.Quantity)
                            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Stock, i => (i.Stock ?? 0) - line.Quantity));
                        if (changed != 1)
                            throw new CheckoutValidationException($"Not enough stock for \"{line.Name}\".");
                    }

                    if (member != null)
                    {
                        var shuRate = await GetShuRateAsync();
                        member.ShuBalance += Math.Round(total * shuRate, 2);
                    }

                    await _context.SaveChangesAsync();
                    await _auditLogService.CreateAsync(new AuditLog
                    {
                        UserId = staffId,
                        UserType = "staff",
                        Action = "checkout",
                        EntityType = "transaction",
                        EntityId = dbTransaction.Id,
                        Details = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            total,
                            payment_method = request.PaymentMethod,
                            member_id = member?.Id
                        }),
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });
                    await transaction.CommitAsync();

                    return new CheckoutResult
                    {
                        TransactionId = dbTransaction.Id,
                        TransactionCode = dbTransaction.TransactionNumber,
                        Total = total
                    };
                }
                catch (Exception ex) when (attempt < maxAttempts && IsRetryableCheckoutException(ex))
                {
                    // MySQL can deadlock the transaction-row insert against
                    // the item update under simultaneous tills. The whole
                    // transaction is rolled back by disposal above; detach
                    // tracked entities before retrying on a clean context.
                    foreach (var entry in _context.ChangeTracker.Entries().ToList())
                        entry.State = EntityState.Detached;
                    await Task.Delay(Random.Shared.Next(25, 100));
                }
                finally
                {
                    if (lockAcquired) await ReleaseTrxNumberLockAsync();
                }
            }
        }

        var discountObj = discount == null
            ? null
            : new
            {
                id = discount.Id,
                name = discount.Name,
                description = discount.Description,
                type = discount.Type,
                value = discount.DiscountRate,
                buy_qty = discount.BuyQty,
                get_qty = discount.GetQty,
                scope = discount.Scope,
                category_id = discount.CategoryId,
                category = discount.Category,
                min_purchase = discount.MinPurchase,
                is_active = discount.IsActive,
                starts_at = discount.StartDate,
                ends_at = discount.EndDate
            };
        var memberObj = member != null
            ? new { id = member.Id, name = member.Name, membership_id = member.Identifier }
            : null;

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

    private static string? ValidateCheckoutRequest(CheckoutRequest request, bool requirePaymentMethod)
    {
        if (request.Items == null || request.Items.Count == 0)
            return "At least one item is required.";
        if (request.Items.Any(i => i.ItemId <= 0 || i.Quantity < 1))
            return "Each item must have a positive item ID and quantity.";
        if (requirePaymentMethod && request.PaymentMethod is not ("cash" or "qris" or "bank_transfer"))
            return "Payment method must be cash, qris, or bank_transfer.";
        return null;
    }

    private static decimal ComputeDiscountAmount(Models.Discount discount, IEnumerable<CheckoutLine> lines, decimal subtotal)
    {
        return discount.Type switch
        {
            "percentage" => subtotal * discount.DiscountRate / 100,
            "flat" or "fixed" => Math.Min(discount.DiscountRate, subtotal),
            "buy_x_get_y" => ComputeBogoAmount(discount, lines),
            _ => 0
        };
    }

    private static decimal ComputeBogoAmount(Models.Discount discount, IEnumerable<CheckoutLine> lines)
    {
        if (!discount.BuyQty.HasValue || !discount.GetQty.HasValue || discount.BuyQty <= 0 || discount.GetQty <= 0)
            return 0;
        var groupSize = discount.BuyQty.Value + discount.GetQty.Value;
        return lines
            .Where(l => !discount.CategoryId.HasValue || l.CategoryId == discount.CategoryId)
            .Sum(l => (decimal)(l.Quantity / groupSize * discount.GetQty.Value) * l.UnitPrice);
    }

    private async Task<decimal> GetShuRateAsync()
    {
        var value = await _context.Settings
            .AsNoTracking()
            .Where(s => s.Key == "shu_rate")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
        return decimal.TryParse(value, out var rate) ? rate : 0.02m;
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

    private static bool IsRetryableCheckoutException(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException!)
        {
            if (current is MySql.Data.MySqlClient.MySqlException mysql &&
                (mysql.Number == 1062 || mysql.Number == 1205 || mysql.Number == 1213))
                return true;
        }
        return false;
    }

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
    public object? Lines { get; set; }
    public decimal? Subtotal { get; set; }
    public object? Discount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public object? Member { get; set; }
    public List<string>? Warnings { get; set; }
}

internal sealed class CheckoutValidationException : Exception
{
    public CheckoutValidationException(string message) : base(message) { }
}

internal sealed class CheckoutLine
{
    public int ItemId { get; init; }
    public string Name { get; init; } = null!;
    public int? CategoryId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }
}
