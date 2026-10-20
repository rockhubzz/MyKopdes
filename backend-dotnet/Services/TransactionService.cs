using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;

namespace MyKopdes.Services;

public class TransactionService : ITransactionService
{
    private readonly KoperasiDbContext _context;

    public TransactionService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<Transaction>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null, string? paymentMethod = null)
    {
        IQueryable<Transaction> query = _context.Transactions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Member)
            .Include(t => t.User)
            .Include(t => t.Items)
            .ThenInclude(ti => ti.Item);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.TransactionNumber.Contains(search) ||
                (t.Member != null && t.Member.Name.Contains(search)) ||
                (t.User != null && t.User.Name.Contains(search)));
        }

        if (!string.IsNullOrEmpty(paymentMethod))
        {
            query = query.Where(t => t.PaymentMethod == paymentMethod);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? search = null, string? paymentMethod = null)
    {
        var query = _context.Transactions.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.TransactionNumber.Contains(search) ||
                (t.Member != null && t.Member.Name.Contains(search)) ||
                (t.User != null && t.User.Name.Contains(search)));
        }

        if (!string.IsNullOrEmpty(paymentMethod))
        {
            query = query.Where(t => t.PaymentMethod == paymentMethod);
        }

        return await query.CountAsync();
    }

    public async Task<bool> VoidAsync(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction == null) return false;
        transaction.PaymentStatus = "void";
        transaction.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        return await _context.Transactions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Member)
            .Include(t => t.User)
            .Include(t => t.Items)
            .ThenInclude(ti => ti.Item)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        transaction.CreatedAt = DateTime.UtcNow;
        transaction.UpdatedAt = DateTime.UtcNow;
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction> UpdateAsync(int id, Transaction transaction)
    {
        var existing = await _context.Transactions.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException("Transaction not found");

        existing.MemberId = transaction.MemberId;
        existing.UserId = transaction.UserId;
        existing.Type = transaction.Type;
        existing.TotalAmount = transaction.TotalAmount;
        existing.PaidAmount = transaction.PaidAmount;
        existing.DiscountAmount = transaction.DiscountAmount;
        existing.TaxAmount = transaction.TaxAmount;
        existing.Notes = transaction.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction == null) return false;
        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Transaction>> GetByMemberAsync(int memberId)
    {
        return await _context.Transactions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Items)
            .ThenInclude(ti => ti.Item)
            .Where(t => t.MemberId == memberId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalSalesAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Transactions
            .Where(t => t.Type == "purchase");

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

        return await query.SumAsync(t => (decimal)t.TotalAmount);
    }

    public async Task<string> GenerateTransactionNumberAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"TRX-{today}-";

        // Allocate from the LATEST existing code via a locking (current) read.
        // A plain COUNT(*) inside this transaction reads the REPEATABLE-READ
        // snapshot taken when the transaction started, so concurrent checkouts
        // would compute the same next number. A `FOR UPDATE` read bypasses the
        // snapshot and also locks that row until commit, which — together with
        // the GET_LOCK advisory lock held by the caller — makes allocation
        // fully serialized.
        var lastCode = (await _context.Database
            .SqlQueryRaw<string?>(
                "SELECT transaction_code AS Value FROM transactions WHERE transaction_code LIKE {0} ORDER BY id DESC LIMIT 1 FOR UPDATE",
                prefix + "%")
            .ToListAsync())
            .FirstOrDefault();

        var next = 1;
        if (lastCode != null && lastCode.Length > prefix.Length &&
            int.TryParse(lastCode[prefix.Length..], out var seq))
        {
            next = seq + 1;
        }

        return $"{prefix}{next:D4}";
    }

    public async Task<Transaction> ProcessPurchaseAsync(int memberId, List<TransactionItem> items, decimal paidAmount, string? notes = null)
    {
        // Serialize transaction-number allocation across concurrent purchases
        // (count-based numbering + UNIQUE index on transaction_code = races).
        const int maxAttempts = 4;
        for (var attempt = 1; ; attempt++)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var lockAcquired = await AcquireTrxNumberLockAsync();
            try
            {
                var txnNumber = await GenerateTransactionNumberAsync();
                var subtotal = items.Sum(i => i.Subtotal);

                var transactionEntity = new Transaction
                {
                    TransactionNumber = txnNumber,
                    MemberId = memberId,
                    Type = "purchase",
                    TotalAmount = subtotal,
                    PaidAmount = paidAmount,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transactionEntity);
                await _context.SaveChangesAsync();

                // Batch-load all purchased items in a single query instead of one
                // FindAsync per line (N+1) — checkout latency matters at the till.
                var itemIds = items.Select(i => i.ItemId).Distinct().ToList();
                var stockById = await _context.Items
                    .Where(i => itemIds.Contains(i.Id))
                    .ToDictionaryAsync(i => i.Id);

                foreach (var item in items)
                {
                    item.TransactionId = transactionEntity.Id;
                    item.CreatedAt = DateTime.UtcNow;
                    _context.TransactionItems.Add(item);

                    // Deduct stock
                    if (stockById.TryGetValue(item.ItemId, out var dbItem))
                        dbItem.Stock -= item.Quantity;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetByIdAsync(transactionEntity.Id) ?? transactionEntity;
            }
            catch (DbUpdateException ex) when (attempt < maxAttempts && IsDuplicateKey(ex))
            {
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

    private static bool IsDuplicateKey(DbUpdateException ex)
        => ex.InnerException is MySqlConnector.MySqlException { Number: 1062 };

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
