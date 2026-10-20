using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/discounts")]
public class DiscountController : ControllerBase
{
    private readonly IDiscountService _discountService;

    public DiscountController(IDiscountService discountService)
    {
        _discountService = discountService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<PaginatedResponse<Discount>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15)
    {
        var discounts = await _discountService.GetAllAsync(page, pageSize);
        var total = await _discountService.GetTotalCountAsync();
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return Ok(new PaginatedResponse<Discount>
        {
            Data = discounts,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Discount>> GetById(int id)
    {
        var discount = await _discountService.GetByIdAsync(id);
        if (discount == null) return NotFound(new { message = "Discount not found" });
        return Ok(discount);
    }

    [HttpGet("active")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Discount>>> GetActive([FromQuery] bool has_member = false)
    {
        var discounts = await _discountService.GetActiveAsync(has_member);
        return Ok(discounts);
    }

    [HttpPost]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Discount>> Create([FromBody] Discount discount)
    {
        var created = await _discountService.CreateAsync(discount);
        return Created($"/api/discounts/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Discount>> Update(int id, [FromBody] Discount discount)
    {
        try
        {
            var updated = await _discountService.UpdateAsync(id, discount);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Discount not found" });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<IActionResult> Delete(int id, [FromQuery(Name = "force")] int force = 0)
    {
        var success = await _discountService.DeleteAsync(id, force != 0);
        if (!success) return NotFound(new { message = "Discount not found" });
        return Ok(new { message = "Discount deleted successfully" });
    }
}
