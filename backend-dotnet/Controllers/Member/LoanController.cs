using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;
using MyKopdes.Middleware;

namespace MyKopdes.Controllers.Member;

[ApiController]
[Route("api/member/loans")]
public class LoanController : ControllerBase
{
    private readonly ILoanService _loanService;
    private readonly ILoanProductService _loanProductService;

    public LoanController(ILoanService loanService, ILoanProductService loanProductService)
    {
        _loanService = loanService;
        _loanProductService = loanProductService;
    }

    [HttpGet]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<PagedResult<Loan>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? status = null)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var pagedLoans = await _loanService.GetPagedByMemberAsync(userId.Value, page, pageSize, status);
        var products = await _loanProductService.GetActiveAsync();

        return Ok(new
        {
            loans = pagedLoans,
            products = products
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<Loan>> GetById(int id)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var loan = await _loanService.GetByIdAsync(id);
        if (loan == null) return NotFound(new { message = "Loan not found" });
        if (loan.MemberId != userId.Value) return Forbid();
        return Ok(loan);
    }

    [HttpPost("apply")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<Loan>> Apply([FromBody] LoanApplyRequest request)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var loanProduct = await _loanProductService.GetByIdAsync(request.LoanProductId);
        if (loanProduct == null || loanProduct.IsActive != true)
            return BadRequest(new { message = "Invalid loan product" });

        var loan = new Loan
        {
            LoanCode = await _loanService.GenerateLoanCodeAsync(),
            MemberId = userId.Value,
            LoanProductId = request.LoanProductId,
            PrincipalAmount = request.Principal,
            TermInMonths = request.TermMonths,
            InterestRate = loanProduct.InterestRate,
            InterestPeriod = loanProduct.InterestPeriod,
            ProcessingFee = 0,
            Status = "pending",
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _loanService.CreateAsync(loan);
        return Created($"/api/member/loans/{created.Id}", created);
    }
}

public class LoanApplyRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("loan_product_id")]
    public int LoanProductId { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("principal")]
    public decimal Principal { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("term_months")]
    public int TermMonths { get; set; }
    public string? Notes { get; set; }
}
