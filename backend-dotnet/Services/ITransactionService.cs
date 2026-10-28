using MyKopdes.Models;

namespace MyKopdes.Services;

public interface ITransactionService
{
    Task<List<Transaction>> GetAllAsync(int page = 1, int pageSize = 15, string? search = null, string? paymentMethod = null,
        DateTime? from = null, DateTime? to = null, int? memberId = null, int? cashierId = null);
    Task<Transaction?> GetByIdAsync(int id);
    Task<Transaction> CreateAsync(Transaction transaction);
    Task<Transaction> UpdateAsync(int id, Transaction transaction);
    Task<bool> DeleteAsync(int id);
    Task<List<Transaction>> GetByMemberAsync(int memberId);
    Task<int> GetTotalCountAsync(string? search = null, string? paymentMethod = null,
        DateTime? from = null, DateTime? to = null, int? memberId = null, int? cashierId = null);
    Task<bool> VoidAsync(int id);
    Task<decimal> GetTotalSalesAsync(DateTime? from = null, DateTime? to = null);
    Task<string> GenerateTransactionNumberAsync();
    Task<Transaction> ProcessPurchaseAsync(int memberId, List<TransactionItem> items, decimal paidAmount, string? notes = null);
}
