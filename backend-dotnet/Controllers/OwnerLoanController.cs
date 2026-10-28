using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers;

// Owner-level loan management: approve/reject, payments, write-offs
[ApiController]
[Route("api/owner/loans")]
public class OwnerLoanController : ControllerBase
{
    private readonly ILoanService _loanService;

    public OwnerLoanController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpGet]
    [Authorize(Roles = "shop_owner,admin")]
    public async Task<ActionResult<PagedResult<Loan>>> GetList(
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
    [Authorize(Roles = "shop_owner,admin")]
    public async Task<ActionResult<Loan>> GetById(int id)
    {
        var loan = await _loanService.GetByIdAsync(id);
        if (loan == null) return NotFound(new { message = "Loan not found" });
        return Ok(loan);
    }

    [HttpPost]
    [Authorize(Roles = "shop_owner,admin")]
    public async Task<ActionResult<Loan>> Create([FromBody] Loan loan)
    {
        var created = await _loanService.CreateAsync(loan);
        return Created($"/api/owner/loans/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "shop_owner,admin")]
    public async Task<ActionResult<Loan>> Update(int id, [FromBody] Loan loan)
    {
        try
        {
            var updated = await _loanService.UpdateAsync(id, loan);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Loan not found" });
        }
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "shop_owner,admin")]
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
    [Authorize(Roles = "shop_owner,admin")]
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

    [HttpPost("{id:int}/payment")]
    [Authorize(Roles = "shop_owner,admin,employee,manager")]
    public async Task<ActionResult<Loan>> AddPayment(int id, [FromBody] LoanPaymentRequest request)
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

    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "shop_owner,admin")]
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
    [Authorize(Roles = "shop_owner,admin")]
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
}
