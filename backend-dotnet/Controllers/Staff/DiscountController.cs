using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;
using System.Text.Json.Serialization;

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
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PaginatedResponse<Discount>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? search = null,
        [FromQuery(Name = "include_inactive")] bool includeInactive = false,
        [FromQuery] string? scope = null,
        [FromQuery(Name = "active_only")] bool activeOnly = false)
    {
        var discounts = await _discountService.GetAllAsync(page, pageSize, search, activeOnly || !includeInactive, scope);
        var total = await _discountService.GetTotalCountAsync(search, activeOnly || !includeInactive, scope);
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
    [Authorize(Roles = "admin")]
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
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Discount>> Create([FromBody] DiscountWriteRequest request)
    {
        var discount = request.ToModel();
        var created = await _discountService.CreateAsync(discount);
        return Created($"/api/discounts/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<Discount>> Update(int id, [FromBody] DiscountWriteRequest request)
    {
        try
        {
            var discount = await _discountService.GetByIdAsync(id);
            if (discount == null) return NotFound(new { message = "Discount not found" });

            request.ApplyTo(discount);
            var updated = await _discountService.UpdateAsync(id, discount);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Discount not found" });
        }
    }

    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Activate(int id)
    {
        var ok = await _discountService.SetActivationAsync(id, true);
        if (!ok) return NotFound(new { message = "Discount not found" });
        return Ok(new { message = "Discount activated successfully" });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id, [FromQuery(Name = "force")] int force = 0)
    {
        try
        {
            var success = await _discountService.DeleteAsync(id, force != 0);
            if (!success) return NotFound(new { message = "Discount not found" });
            return Ok(new { message = "Discount deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}

public sealed class DiscountWriteRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    [JsonPropertyName("discount_type")]
    public string? LegacyType { get; set; }
    public decimal? Value { get; set; }
    public int? BuyQty { get; set; }
    public int? GetQty { get; set; }
    public string? Scope { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPurchase { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    [JsonPropertyName("start_date")]
    public DateTime? LegacyStartDate { get; set; }
    [JsonPropertyName("end_date")]
    public DateTime? LegacyEndDate { get; set; }

    public Discount ToModel() => new()
    {
        Name = Name ?? "",
        Description = Description,
        Type = Type ?? LegacyType ?? "percentage",
        DiscountRate = Value ?? 0,
        BuyQty = BuyQty,
        GetQty = GetQty,
        Scope = Scope ?? "general",
        CategoryId = CategoryId,
        MinPurchase = MinPurchase,
        IsActive = IsActive ?? true,
        StartDate = StartsAt ?? LegacyStartDate,
        EndDate = EndsAt ?? LegacyEndDate
    };

    public void ApplyTo(Discount target)
    {
        if (Name != null) target.Name = Name;
        if (Description != null) target.Description = Description;
        if (Type != null || LegacyType != null) target.Type = Type ?? LegacyType!;
        if (Value.HasValue) target.DiscountRate = Value.Value;
        if (BuyQty.HasValue) target.BuyQty = BuyQty;
        if (GetQty.HasValue) target.GetQty = GetQty;
        if (Scope != null) target.Scope = Scope;
        if (CategoryId.HasValue) target.CategoryId = CategoryId;
        if (MinPurchase.HasValue) target.MinPurchase = MinPurchase;
        if (IsActive.HasValue) target.IsActive = IsActive.Value;
        if (StartsAt.HasValue || LegacyStartDate.HasValue) target.StartDate = StartsAt ?? LegacyStartDate;
        if (EndsAt.HasValue || LegacyEndDate.HasValue) target.EndDate = EndsAt ?? LegacyEndDate;
    }
}
