using Microsoft.EntityFrameworkCore;
using MyKopdes.Data;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;

namespace MyKopdes.Services;

public class LoanService : ILoanService
{
    private readonly KoperasiDbContext _context;

    public LoanService(KoperasiDbContext context)
    {
        _context = context;
    }

    public async Task<List<Loan>> GetAllAsync(int page = 1, int pageSize = 15)
    {
        return await _context.Loans
            .Include(l => l.Member)
            .Include(l => l.LoanProduct)
            .Include(l => l.User)
            .Include(l => l.Payments)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Loan?> GetByIdAsync(int id)
    {
        return await _context.Loans
            .Include(l => l.Member)
            .Include(l => l.LoanProduct)
            .Include(l => l.User)
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<Loan>> GetByMemberAsync(int memberId)
    {
        return await _context.Loans
            .Include(l => l.LoanProduct)
            .Include(l => l.Payments)
            .Where(l => l.MemberId == memberId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Loan>> GetByStatusAsync(string status)
    {
        return await _context.Loans
            .Include(l => l.Member)
            .Include(l => l.LoanProduct)
            .Include(l => l.Payments)
            .Where(l => l.Status == status)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<string> GenerateLoanCodeAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Loans.CountAsync(l => l.CreatedAt >= DateTime.UtcNow.Date);
        var nextNumber = count + 1;
        return $"LN-{today}-{nextNumber:D4}";
    }

    public Task<decimal> CalculateInterestAsync(decimal principal, int termMonths, decimal interestRate)
    {
        // interestRate is a decimal (e.g. 0.01 = 1% per month)
        var result = Math.Round(principal * interestRate * termMonths, 2);
        return Task.FromResult(result);
    }

    public async Task<Loan> CreateAsync(Loan loan)
    {
        if (string.IsNullOrEmpty(loan.LoanCode))
            loan.LoanCode = await GenerateLoanCodeAsync();

        // Calculate interest and payments if not set
        loan.InterestAmount = await CalculateInterestAsync(loan.PrincipalAmount ?? 0, loan.TermInMonths, loan.InterestRate ?? 0);

        loan.TotalAmount = (loan.PrincipalAmount ?? 0) + (loan.InterestAmount ?? 0);
        loan.RemainingAmount = loan.TotalAmount;

        if (loan.TermInMonths > 0 && loan.TotalAmount > 0)
        {
            loan.MonthlyPayment = Math.Round(loan.TotalAmount.Value / loan.TermInMonths, 2);
        }

        loan.CreatedAt = DateTime.UtcNow;
        loan.UpdatedAt = DateTime.UtcNow;
        loan.Status = "pending";
        loan.PaidAmount = loan.PaidAmount ?? 0;
        loan.ProcessingFee = loan.ProcessingFee ?? 0;

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        // Update member loan balance
        var member = await _context.Members.FindAsync(loan.MemberId);
        if (member != null)
        {
            member.LoanBalance = (member.LoanBalance ?? 0) + loan.PrincipalAmount;
            await _context.SaveChangesAsync();
        }

        return await GetByIdAsync(loan.Id) ?? loan;
    }

    public async Task<Loan> UpdateAsync(int id, Loan loan)
    {
        var existing = await _context.Loans.FindAsync(id);
        if (existing == null) throw new KeyNotFoundException("Loan not found");

        existing.LoanProductId = loan.LoanProductId;
        existing.PrincipalAmount = loan.PrincipalAmount;
        existing.InterestAmount = loan.InterestAmount;
        existing.InterestRate = loan.InterestRate;
        existing.InterestPeriod = loan.InterestPeriod;
        existing.TermInMonths = loan.TermInMonths;
        existing.MonthlyPayment = loan.MonthlyPayment;
        existing.TotalAmount = loan.TotalAmount;
        existing.RemainingAmount = loan.RemainingAmount;
        existing.LoanDate = loan.LoanDate;
        existing.DueDate = loan.DueDate;
        existing.Status = loan.Status;
        existing.Notes = loan.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.Loans.Update(existing);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? existing;
    }

    public async Task<Loan> ApproveAsync(int id, int userId)
    {
        var loan = await _context.Loans
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) throw new KeyNotFoundException("Loan not found");

        loan.Status = "approved";
        loan.UserId = userId;
        loan.ApprovedAt = DateTime.UtcNow;
        loan.UpdatedAt = DateTime.UtcNow;

        _context.Loans.Update(loan);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? loan;
    }

    public async Task<Loan> RejectAsync(int id, string? reason = null)
    {
        var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) throw new KeyNotFoundException("Loan not found");

        loan.Status = "rejected";
        loan.UpdatedAt = DateTime.UtcNow;

        _context.Loans.Update(loan);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? loan;
    }

    public async Task<Loan> AddPaymentAsync(int loanId, decimal amount, int? userId = null, string? notes = null, string? paymentMethod = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        var loan = await _context.Loans
            .Include(l => l.Payments)
            .FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan == null) throw new KeyNotFoundException("Loan not found");

        var payment = new LoanPayment
        {
            LoanId = loanId,
            UserId = userId,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            Notes = notes,
            PaymentMethod = paymentMethod,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LoanPayments.Add(payment);

        loan.PaidAmount = (loan.PaidAmount ?? 0) + amount;
        loan.RemainingAmount = (loan.RemainingAmount ?? 0) - amount;

        if (loan.RemainingAmount <= 0)
        {
            loan.Status = "paid_off";
            loan.PaidAt = DateTime.UtcNow;
        }

        loan.UpdatedAt = DateTime.UtcNow;

        _context.Loans.Update(loan);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(loanId) ?? loan;
    }

    public async Task<Loan> WriteOffAsync(int id, string? reason = null)
    {
        var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) throw new KeyNotFoundException("Loan not found");

        loan.Status = "written_off";
        loan.RemainingAmount = 0;
        loan.UpdatedAt = DateTime.UtcNow;

        _context.Loans.Update(loan);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id) ?? loan;
    }

    public async Task<PagedResult<Loan>> GetPagedAsync(int page, int pageSize, string? status = null, string? search = null, int? memberId = null)
    {
        var query = _context.Loans
            .Include(l => l.Member)
            .Include(l => l.LoanProduct)
            .Include(l => l.Payments)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);

        if (memberId.HasValue)
            query = query.Where(l => l.MemberId == memberId.Value);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l => l.LoanCode.Contains(search) || l.LoanProduct.Name.Contains(search));
        }

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Loan>
        {
            Data = data,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        };
    }

    public async Task<PagedResult<Loan>> GetPagedByMemberAsync(int memberId, int page, int pageSize, string? status = null)
    {
        var query = _context.Loans
            .Include(l => l.Member)
            .Include(l => l.LoanProduct)
            .Include(l => l.Payments)
            .Where(l => l.MemberId == memberId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Loan>
        {
            Data = data,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        };
    }
}
