using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Staff;

// Frontend-compatible alias. The original /api/restocks route remains in
// place for existing API clients.
[ApiController]
[Route("api/restocking-records")]
public class RestockingRecordsController : ControllerBase
{
    private readonly IRestockService _restockService;

    public RestockingRecordsController(IRestockService restockService)
    {
        _restockService = restockService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<PaginatedResponse<RestockingRecordResponse>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] int? item_id = null,
        [FromQuery(Name = "mine_only")] bool mineOnly = false)
    {
        int? userId = mineOnly ? User.GetUserIdOrThrow() : null;
        var (records, total) = await _restockService.GetPagedAsync(page, pageSize, item_id, userId);

        return Ok(new PaginatedResponse<RestockingRecordResponse>
        {
            Data = records.Select(RestockingRecordResponse.From).ToList(),
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<RestockingRecordResponse>> GetById(int id)
    {
        var record = await _restockService.GetByIdAsync(id);
        if (record == null) return NotFound(new { message = "Restock record not found" });
        return Ok(RestockingRecordResponse.From(record));
    }

    [HttpPost]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<RestockingRecordResponse>> Create([FromBody] RestockCreateRequest request)
    {
        var userId = User.GetUserIdOrThrow();
        var record = new RestockingRecord
        {
            ItemId = request.ItemId,
            SupplierId = request.SupplierId,
            UserId = userId,
            Quantity = request.Quantity,
            CostPerUnit = request.CostPerUnit,
            TotalCost = request.Quantity * request.CostPerUnit,
            Notes = request.Notes,
            RestockDate = request.RestockedAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _restockService.CreateAsync(record, userId);
        return Created($"/api/restocking-records/{created.Id}", RestockingRecordResponse.From(created));
    }
}
