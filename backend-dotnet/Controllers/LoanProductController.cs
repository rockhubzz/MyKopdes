using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers;

[ApiController]
[Route("api/loan-products")]
public class LoanProductController : ControllerBase
{
    private readonly ILoanProductService _loanProductService;

    public LoanProductController(ILoanProductService loanProductService)
    {
        _loanProductService = loanProductService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<PaginatedResponse<LoanProduct>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15)
    {
        var products = await _loanProductService.GetAllAsync(page, pageSize);
        var total = await _loanProductService.GetTotalCountAsync();
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return Ok(new PaginatedResponse<LoanProduct>
        {
            Data = products,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<LoanProduct>> GetById(int id)
    {
        var product = await _loanProductService.GetByIdAsync(id);
        if (product == null) return NotFound(new { message = "Loan product not found" });
        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "admin,owner,shop_owner,employee")]
    public async Task<ActionResult<LoanProduct>> Create([FromBody] LoanProduct product)
    {
        var created = await _loanProductService.CreateAsync(product);
        return Created($"/api/loan-products/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,owner,shop_owner,employee")]
    public async Task<ActionResult<LoanProduct>> Update(int id, [FromBody] LoanProduct product)
    {
        try
        {
            var updated = await _loanProductService.UpdateAsync(id, product);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Loan product not found" });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,owner,shop_owner,employee")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _loanProductService.DeleteAsync(id);
        if (!success) return NotFound(new { message = "Loan product not found" });
        return Ok(new { message = "Loan product deleted successfully" });
    }

    [HttpGet("settings")]
    [Authorize(Roles = "admin,owner,shop_owner,employee")]
    public async Task<ActionResult<IEnumerable<LoanSetting>>> GetSettings()
    {
        var settings = await _loanProductService.GetSettingsAsync();
        return Ok(settings);
    }

    [HttpPut("settings/{key}")]
    [Authorize(Roles = "admin,owner,shop_owner,employee")]
    public async Task<ActionResult<LoanSetting>> UpdateSetting(string key, [FromBody] SettingUpdateRequest request)
    {
        var setting = await _loanProductService.UpdateSettingAsync(key, request.Value);
        return Ok(setting);
    }
}

public class SettingUpdateRequest
{
    public string Value { get; set; } = null!;
}
