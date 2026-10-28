using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/suppliers")]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<PaginatedResponse<Supplier>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? search = null)
    {
        var suppliers = await _supplierService.GetAllAsync(page, pageSize, search);
        var total = await _supplierService.GetTotalCountAsync(search);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return Ok(new PaginatedResponse<Supplier>
        {
            Data = suppliers,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<Supplier>> GetById(int id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);
        if (supplier == null) return NotFound(new { message = "Supplier not found" });
        return Ok(supplier);
    }

    [HttpPost]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<Supplier>> Create([FromBody] Supplier supplier)
    {
        var created = await _supplierService.CreateAsync(supplier);
        return Created($"/api/suppliers/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<Supplier>> Update(int id, [FromBody] Supplier supplier)
    {
        try
        {
            var updated = await _supplierService.UpdateAsync(id, supplier);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Supplier not found" });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _supplierService.DeleteAsync(id);
            if (!success) return NotFound(new { message = "Supplier not found" });
            return Ok(new { message = "Supplier deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpGet("search/{query}")]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<IEnumerable<Supplier>>> Search(string query)
    {
        var suppliers = await _supplierService.SearchAsync(query);
        return Ok(suppliers);
    }
}
