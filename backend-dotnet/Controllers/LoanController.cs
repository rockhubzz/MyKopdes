using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers;

[ApiController]
[Route("api/loans")]
public class LoanController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoanController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<PagedResult<Loan>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int? member_id = null)
    {
        var result = await _loanService.GetPagedAsync(page, pageSize, status, search, member_id);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<Loan>> GetById(int id)
    {
        var loan = await _loanService.GetByIdAsync(id);
        if (loan == null) return NotFound(new { message = "Loan not found" });
        return Ok(loan);
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<Loan>> Approve(int id)
    {
        var userId = User.GetUserIdOrThrow();
        try
        {
            var loan = await _loanService.ApproveAsync(id, userId);
            return Ok(loan);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Loan not found" });
        }
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<Loan>> Reject(int id, [FromBody] LoanActionRequest? request = null)
    {
        try
        {
            var loan = await _loanService.RejectAsync(id, request?.Reason);
            return Ok(loan);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Loan not found" });
        }
    }

    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<Loan>> Activate(int id)
    {
        try
        {
            var loan = await _loanService.ActivateAsync(id);
            return Ok(loan);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Loan not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/write-off")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<Loan>> WriteOff(int id, [FromBody] LoanActionRequest? request = null)
    {
        try
        {
            var loan = await _loanService.WriteOffAsync(id, request?.Reason);
            return Ok(loan);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Loan not found" });
        }
    }

    [HttpPost("{id:int}/pay")]
    [Authorize(Roles = "admin,shop_owner,employee,manager")]
    public async Task<ActionResult<Loan>> Pay(int id, [FromBody] LoanPaymentRequest request)
    {
        var userId = User.GetUserIdOrThrow();
        try
        {
            var loan = await _loanService.AddPaymentAsync(id, request.Amount, userId, request.Notes, request.PaymentMethod);
            return Ok(loan);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Loan not found" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class LoanActionRequest
{
    public string? Reason { get; set; }
}

public class LoanPaymentRequest
{
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public string? PaymentMethod { get; set; }
}
