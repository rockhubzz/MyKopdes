using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;
using System.ComponentModel.DataAnnotations;

namespace MyKopdes.Controllers.Staff;

[ApiController]
[Route("api/restocks")]
public class RestockController : ControllerBase
{
    private readonly IRestockService _restockService;

    public RestockController(IRestockService restockService)
    {
        _restockService = restockService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<IEnumerable<RestockingRecordResponse>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        var records = await _restockService.GetAllAsync(page, pageSize);
        return Ok(records.Select(RestockingRecordResponse.From));
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
        return Created($"/api/restocks/{created.Id}", RestockingRecordResponse.From(created));
    }

    [HttpGet("item/{itemId:int}")]
    [Authorize(Roles = "admin,shop_owner,manager,employee")]
    public async Task<ActionResult<IEnumerable<RestockingRecordResponse>>> GetByItem(int itemId)
    {
        var records = await _restockService.GetByItemAsync(itemId);
        return Ok(records.Select(RestockingRecordResponse.From));
    }
}

public class RestockCreateRequest
{
    [Range(1, int.MaxValue)]
    public int ItemId { get; set; }
    public int? SupplierId { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    [Range(0, double.MaxValue)]
    public decimal CostPerUnit { get; set; }
    public string? Notes { get; set; }
    public DateTime? RestockedAt { get; set; }
}
