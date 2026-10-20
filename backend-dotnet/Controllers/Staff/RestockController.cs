using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Services;

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
    [Authorize(Roles = "admin,owner,manager")]
    public async Task<ActionResult<IEnumerable<RestockingRecord>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        var records = await _restockService.GetAllAsync(page, pageSize);
        return Ok(records);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,owner,manager")]
    public async Task<ActionResult<RestockingRecord>> GetById(int id)
    {
        var record = await _restockService.GetByIdAsync(id);
        if (record == null) return NotFound(new { message = "Restock record not found" });
        return Ok(record);
    }

    [HttpPost]
    [Authorize(Roles = "admin,owner,manager")]
    public async Task<ActionResult<RestockingRecord>> Create([FromBody] RestockCreateRequest request)
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
            RestockDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _restockService.CreateAsync(record, userId);
        return Created($"/api/restocks/{created.Id}", created);
    }

    [HttpGet("item/{itemId:int}")]
    [Authorize(Roles = "admin,owner,manager")]
    public async Task<ActionResult<IEnumerable<RestockingRecord>>> GetByItem(int itemId)
    {
        var records = await _restockService.GetByItemAsync(itemId);
        return Ok(records);
    }
}

public class RestockCreateRequest
{
    public int ItemId { get; set; }
    public int SupplierId { get; set; }
    public int Quantity { get; set; }
    public decimal CostPerUnit { get; set; }
    public string? Notes { get; set; }
}
