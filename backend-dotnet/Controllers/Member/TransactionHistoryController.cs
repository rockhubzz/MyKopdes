using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyKopdes.Services;

namespace MyKopdes.Controllers.Member;

[ApiController]
[Route("api/member")]
public class TransactionHistoryController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionHistoryController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet("transactions")]
    [Authorize(Roles = "member")]
    public async Task<ActionResult<IEnumerable<object>>> GetTransactions()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized(new { message = "Not authenticated" });

        var transactions = await _transactionService.GetByMemberAsync(userId.Value);
        return Ok(transactions);
    }
}
