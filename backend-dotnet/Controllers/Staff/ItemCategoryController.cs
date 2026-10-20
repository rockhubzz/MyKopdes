using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;
using MyKopdes.Data;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/item-categories")]
public class ItemCategoryController : ControllerBase
{
    private readonly IItemCategoryService _categoryService;
    private readonly KoperasiDbContext _context;

    public ItemCategoryController(IItemCategoryService categoryService, KoperasiDbContext context)
    {
        _categoryService = categoryService;
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<PaginatedResponse<ItemCategory>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? search = null)
    {
        var categories = await _categoryService.GetAllAsync(page, pageSize, search);
        var total = await _categoryService.GetTotalCountAsync(search);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return Ok(new PaginatedResponse<ItemCategory>
        {
            Data = categories,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<ItemCategory>> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null) return NotFound(new { message = "Category not found" });
        return Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<ItemCategory>> Create([FromBody] ItemCategory category)
    {
        var existing = await _context.ItemCategories.FirstOrDefaultAsync(c => c.Name == category.Name);
        if (existing != null) return BadRequest(new { message = "Category name already exists" });
        var created = await _categoryService.CreateAsync(category);
        return Created($"/api/categories/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<ItemCategory>> Update(int id, [FromBody] ItemCategory category)
    {
        try
        {
            var updated = await _categoryService.UpdateAsync(id, category);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Category not found" });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var success = await _categoryService.DeleteAsync(id);
            if (!success) return NotFound(new { message = "Category not found" });
            return Ok(new { message = "Category deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}
