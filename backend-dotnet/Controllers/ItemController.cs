using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers;

[ApiController]
[Route("api/items")]
public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;
    private readonly IItemCategoryService _categoryService;
    private readonly ISupplierService _supplierService;
    private readonly IDiscountService _discountService;
    private readonly IFileUploadService _fileUploads;

    public ItemController(IItemService itemService, IItemCategoryService categoryService,
        ISupplierService supplierService, IDiscountService discountService,
        IFileUploadService fileUploads)
    {
        _itemService = itemService;
        _categoryService = categoryService;
        _supplierService = supplierService;
        _discountService = discountService;
        _fileUploads = fileUploads;
    }

    [HttpGet]
    [Authorize(Roles = "admin,owner,manager,member,shop_owner,employee")]
    public async Task<ActionResult<PaginatedResponse<Item>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? search = null,
        [FromQuery] int? category_id = null,
        [FromQuery] bool low_stock = false,
        [FromQuery] bool include_inactive = false)
    {
        var items = await _itemService.GetAllAsync(page, pageSize, search, category_id, low_stock, !include_inactive);
        var total = await _itemService.GetTotalCountAsync(search, category_id, low_stock, !include_inactive);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return Ok(new PaginatedResponse<Item>
        {
            Data = items,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,member,shop_owner,employee")]
    public async Task<ActionResult<Item>> GetById(int id)
    {
        var item = await _itemService.GetByIdAsync(id);
        if (item == null) return NotFound(new { message = "Item not found" });
        return Ok(item);
    }

    [HttpGet("scan")]
    [Authorize]
    public async Task<ActionResult<Item>> Scan([FromQuery] string code)
    {
        var item = await _itemService.ScanAsync(code);
        if (item == null) return NotFound(new { message = "Item not found." });
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Item>> Create()
    {
        var form = await Request.ReadFormAsync();
        var item = new Item
        {
            Name = form["name"],
            Code = form["sku"],
            Barcode = form["barcode"],
            CategoryId = int.TryParse(form["category_id"], out var cat) && cat > 0 ? cat : null,
            Price = decimal.TryParse(form["unit_price"], out var price) ? price : null,
            Cost = decimal.TryParse(form["cost_price"], out var cost) ? cost : null,
            Stock = int.TryParse(form["current_stock"], out var stock) ? stock : 0,
            MinStock = int.TryParse(form["min_stock_threshold"], out var minStock) ? minStock : 0,
            Unit = form["unit_of_measure"] == "" ? "pcs" : form["unit_of_measure"],
            ExpiryDate = DateTime.TryParse(form["expiry_date"], out var exp) ? exp : null,
            IsActive = form["is_active"] != "false" && form["is_active"] != "0",
            Image = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Image stored as a storage-relative path ("items/…") so the frontend
        // /storage/{image_path} URL resolves via the static-files middleware.
        var files = Request.Form.Files;
        if (files.Count > 0 && files[0].Length > 0)
        {
            var saved = await _fileUploads.SaveImageAsync(files[0], "items", "item");
            if (saved == null)
                return BadRequest(new { message = "Unsupported image type or file too large (jpg/png/webp/gif, max 10MB)" });
            item.Image = saved;
        }

        var created = await _itemService.CreateAsync(item);
        return Created($"/api/items/{created.Id}", created);
    }

    [HttpPost("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Item>> CreateOrUpdate(int id)
    {
        // Laravel-style _method spoofing: frontend sends POST with _method=PUT
        var form = await Request.ReadFormAsync();
        if (form["_method"] == "PUT")
        {
            return await Update(id, form);
        }
        // No _method override: treat as create on wrong route
        return BadRequest(new { message = "Use POST /api/items for creation" });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Item>> Update(int id, IFormCollection form = null!)
    {
        try
        {
            if (form == null) form = await Request.ReadFormAsync();
            var uom = form["unit_of_measure"].ToString();
            var item = new Item
            {
                Name = form["name"],
                Code = form["sku"],
                Barcode = form["barcode"],
                CategoryId = int.TryParse(form["category_id"], out var cat) && cat > 0 ? cat : null,
                Price = decimal.TryParse(form["unit_price"], out var price) ? price : null,
                Cost = decimal.TryParse(form["cost_price"], out var cost) ? cost : null,
                Stock = int.TryParse(form["current_stock"], out var stock) ? stock : 0,
                MinStock = int.TryParse(form["min_stock_threshold"], out var minStock) ? minStock : 0,
                Unit = string.IsNullOrEmpty(uom) ? "pcs" : uom,
                ExpiryDate = DateTime.TryParse(form["expiry_date"], out var exp) ? exp : null,
                IsActive = form["is_active"] == "" || form["is_active"] == "true" || form["is_active"] == "1",
            };

            // Image stored as a storage-relative path ("items/…") so the
            // frontend /storage/{image_path} URL resolves; the replaced file
            // is deleted so uploads don't accumulate.
            var files = Request.Form.Files;
            if (files.Count > 0 && files[0].Length > 0)
            {
                var saved = await _fileUploads.SaveImageAsync(files[0], "items", "item");
                if (saved == null)
                    return BadRequest(new { message = "Unsupported image type or file too large (jpg/png/webp/gif, max 10MB)" });
                item.Image = saved;
            }

            // Only update fields that were provided in the form
            var existing = await _itemService.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Item not found" });

            if (!string.IsNullOrEmpty(form["name"])) existing.Name = item.Name;
            if (!string.IsNullOrEmpty(form["sku"])) existing.Code = item.Code;
            if (!string.IsNullOrEmpty(form["barcode"])) existing.Barcode = item.Barcode;
            if (!string.IsNullOrEmpty(form["category_id"])) existing.CategoryId = item.CategoryId;
            if (!string.IsNullOrEmpty(form["unit_price"])) existing.Price = item.Price;
            if (!string.IsNullOrEmpty(form["cost_price"])) existing.Cost = item.Cost;
            if (!string.IsNullOrEmpty(form["current_stock"])) existing.Stock = item.Stock;
            if (!string.IsNullOrEmpty(form["min_stock_threshold"])) existing.MinStock = item.MinStock;
            if (!string.IsNullOrEmpty(form["unit_of_measure"])) existing.Unit = item.Unit;
            if (!string.IsNullOrEmpty(form["expiry_date"])) existing.ExpiryDate = item.ExpiryDate;
            if (!string.IsNullOrEmpty(form["is_active"])) existing.IsActive = item.IsActive;
            if (item.Image != null)
            {
                // Replace: remove the superseded file first.
                _fileUploads.DeleteStoredImage(existing.Image);
                existing.Image = item.Image;
            }

            await _itemService.UpdateAsync(id, existing);
            return Ok(existing);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Item not found" });
        }
    }

    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<Item>> Activate(int id)
    {
        var item = await _itemService.ActivateAsync(id);
        if (item == null) return NotFound(new { message = "Item not found" });
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<IActionResult> Delete(int id, [FromQuery(Name = "force")] int force = 0)
    {
        var success = await _itemService.DeleteAsync(id, force != 0);
        if (!success) return NotFound(new { message = "Item not found" });
        return Ok(new { message = "Item deleted successfully" });
    }

    [HttpGet("search/{query}")]
    [Authorize(Roles = "admin,owner,manager,member,shop_owner,employee")]
    public async Task<ActionResult<IEnumerable<Item>>> Search(string query)
    {
        var items = await _itemService.SearchAsync(query);
        return Ok(items);
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<IEnumerable<Item>>> GetLowStock([FromQuery] int threshold = 10)
    {
        var items = await _itemService.GetLowStockAsync(threshold);
        return Ok(items);
    }

    [HttpGet("count")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<object>> GetCount()
    {
        var count = await _itemService.GetTotalCountAsync();
        return Ok(new { count });
    }
}
