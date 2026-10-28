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
    [Authorize(Roles = "admin,shop_owner")]
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
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<MemberModel>> GetById(int id)
    {
        var member = await _memberService.GetByIdAsync(id);
        if (member == null) return NotFound(new { message = "Member not found" });
        return Ok(member);
    }

    [HttpPost]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<MemberModel>> Create([FromBody] MemberCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Name and password are required" });
        if (!string.IsNullOrWhiteSpace(request.Email) && await _memberService.GetByEmailAsync(request.Email.Trim()) != null)
            return Conflict(new { message = "Email already in use" });
        if (!string.IsNullOrWhiteSpace(request.Phone) && await _memberService.GetByIdentifierAsync(request.Phone.Trim()) != null)
            return Conflict(new { message = "Phone already in use" });

        var member = new MemberModel
        {
            Name = request.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
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
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<MemberModel>> Update(int id, [FromBody] MemberUpdateRequest request)
    {
        try
        {
            var existing = await _memberService.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Member not found" });

            if (request.Name != null) existing.Name = request.Name.Trim();
            if (request.Email != null)
            {
                var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
                var duplicate = email == null ? null : await _memberService.GetByEmailAsync(email);
                if (duplicate != null && duplicate.Id != id)
                    return Conflict(new { message = "Email already in use" });
                existing.Email = email;
            }
            if (request.Phone != null)
            {
                var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
                var duplicate = phone == null ? null : await _memberService.GetByIdentifierAsync(phone);
                if (duplicate != null && duplicate.Id != id)
                    return Conflict(new { message = "Phone already in use" });
                existing.Phone = phone;
            }
            if (request.Address != null) existing.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address;
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
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<IActionResult> Delete(int id, [FromQuery(Name = "force")] int force = 0)
    {
        try
        {
            var success = await _memberService.DeleteAsync(id, force != 0);
            if (!success) return NotFound(new { message = "Member not found" });
            return Ok(new { message = "Member deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpGet("search/{query}")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<IEnumerable<MemberModel>>> Search(string query)
    {
        var members = await _memberService.SearchAsync(query);
        return Ok(members);
    }

    [HttpGet("count")]
    [Authorize(Roles = "admin,shop_owner")]
    public async Task<ActionResult<object>> GetCount()
    {
        var count = await _memberService.GetTotalCountAsync();
        return Ok(new { count });
    }

    [HttpGet("lookup")]
    [Authorize(Roles = "admin,shop_owner,employee")]
    public async Task<ActionResult<IEnumerable<MemberModel>>> Lookup([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest(new { message = "Query is required" });
        var members = await _memberService.LookupAsync(query);
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
