using MyKopdes.Models;
using MyKopdes.Models.DTOs;

namespace MyKopdes.Services;

public interface ILoanService
{
    Task<List<Loan>> GetAllAsync(int page = 1, int pageSize = 15);
    Task<Loan?> GetByIdAsync(int id);
    Task<List<Loan>> GetByMemberAsync(int memberId);
    Task<Loan> CreateAsync(Loan loan);
    Task<Loan> UpdateAsync(int id, Loan loan);
    Task<Loan> ApproveAsync(int id, int userId);
    Task<Loan> RejectAsync(int id, string? reason = null);
    Task<Loan> AddPaymentAsync(int loanId, decimal amount, int? userId = null, string? notes = null, string? paymentMethod = null);
    Task<Loan> WriteOffAsync(int id, string? reason = null);
    Task<List<Loan>> GetByStatusAsync(string status);
    Task<string> GenerateLoanCodeAsync();
    Task<decimal> CalculateInterestAsync(decimal principal, int termMonths, decimal interestRate);
    Task<PagedResult<Loan>> GetPagedAsync(int page, int pageSize, string? status = null, string? search = null, int? memberId = null);
    Task<PagedResult<Loan>> GetPagedByMemberAsync(int memberId, int page, int pageSize, string? status = null);
}
