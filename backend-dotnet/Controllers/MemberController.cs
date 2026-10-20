using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Models;
using MyKopdes.Models.DTOs;
using MyKopdes.Services;

namespace MyKopdes.Controllers;

using MemberModel = MyKopdes.Models.Member;

[ApiController]
[Route("api/members")]
public class MemberController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MemberController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpGet]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<PaginatedResponse<MemberModel>>> Get(
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int pageSize = 15,
        [FromQuery] string? search = null,
        [FromQuery(Name = "include_inactive")] bool includeInactive = false)
    {
        var members = await _memberService.GetAllAsync(page, pageSize, search, !includeInactive);
        var total = await _memberService.GetTotalCountAsync(search, !includeInactive);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);
        return Ok(new PaginatedResponse<MemberModel>
        {
            Data = members,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<MemberModel>> GetById(int id)
    {
        var member = await _memberService.GetByIdAsync(id);
        if (member == null) return NotFound(new { message = "Member not found" });
        return Ok(member);
    }

    [HttpPost]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<MemberModel>> Create([FromBody] MemberCreateRequest request)
    {
        var member = new MemberModel
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            IsActive = true,
            Password = request.Password != null ? BCrypt.Net.BCrypt.HashPassword(request.Password) : string.Empty,
            ShuBalance = 0,
            Locale = "en",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _memberService.CreateAsync(member);
        return Created($"/api/members/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<MemberModel>> Update(int id, [FromBody] MemberUpdateRequest request)
    {
        try
        {
            var existing = await _memberService.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Member not found" });

            if (!string.IsNullOrEmpty(request.Name)) existing.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Email)) existing.Email = request.Email;
            if (!string.IsNullOrEmpty(request.Phone)) existing.Phone = request.Phone;
            if (!string.IsNullOrEmpty(request.Address)) existing.Address = request.Address;
            if (request.IsActive.HasValue) existing.IsActive = request.IsActive.Value;
            if (!string.IsNullOrEmpty(request.Locale)) existing.Locale = request.Locale;
            if (!string.IsNullOrEmpty(request.Password)) existing.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _memberService.UpdateAsync(id, existing);
            return Ok(existing);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Member not found" });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<IActionResult> Delete(int id, [FromQuery(Name = "force")] int force = 0)
    {
        var success = await _memberService.DeleteAsync(id, force != 0);
        if (!success) return NotFound(new { message = "Member not found" });
        return Ok(new { message = "Member deleted successfully" });
    }

    [HttpGet("search/{query}")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<IEnumerable<MemberModel>>> Search(string query)
    {
        var members = await _memberService.SearchAsync(query);
        return Ok(members);
    }

    [HttpGet("count")]
    [Authorize(Roles = "admin,owner,manager,shop_owner,employee")]
    public async Task<ActionResult<object>> GetCount()
    {
        var count = await _memberService.GetTotalCountAsync();
        return Ok(new { count });
    }

    [HttpGet("lookup")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MemberModel>>> Lookup([FromQuery] string query)
    {
        var members = await _memberService.SearchAsync(query);
        return Ok(members);
    }
}

public class MemberCreateRequest
{
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Password { get; set; }
}

public class MemberUpdateRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool? IsActive { get; set; }
    public string? Locale { get; set; }
    public string? Password { get; set; }
}
