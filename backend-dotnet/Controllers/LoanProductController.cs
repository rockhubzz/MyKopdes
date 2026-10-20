using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;
using SettingUpdateRequest = MyKopdes.Controllers.Staff.SettingUpdateRequest;

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
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? search = null,
        [FromQuery(Name = "include_inactive")] bool includeInactive = false)
    {
        var products = await _loanProductService.GetAllAsync(page, pageSize, search, !includeInactive);
        var total = await _loanProductService.GetTotalCountAsync(search, !includeInactive);
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

    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "admin,owner,shop_owner,employee")]
    public async Task<IActionResult> Activate(int id)
    {
        var ok = await _loanProductService.SetActivationAsync(id, true);
        if (!ok) return NotFound(new { message = "Loan product not found" });
        return Ok(new { message = "Loan product activated successfully" });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,owner,shop_owner,employee")]
    public async Task<IActionResult> Delete(int id, [FromQuery(Name = "force")] int force = 0)
    {
        var success = await _loanProductService.DeleteAsync(id, force != 0);
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

// NOTE: no local SettingUpdateRequest class here — this controller reuses the
// one defined in Controllers.Staff. Two same-named DTOs in different
// namespaces collide on Swagger schema ids and 500 the swagger.json doc.
