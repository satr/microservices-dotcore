using Asp.Versioning;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/inventory")]
[Authorize] // any authenticated user can read stock; librarian-managed writes would go here
public sealed class InventoryController : ControllerBase
{
    private readonly IBookInventoryService _inventory;

    public InventoryController(IBookInventoryService inventory)
    {
        _inventory = inventory;
    }

    [HttpGet("stock/{bookId}")]
    public async Task<IActionResult> GetStock(string bookId)
    {
        var stock = await _inventory.GetStockAsync(bookId);
        return Ok(new { bookId, stock });
    }

    [HttpPost("stock/batch")]
    public async Task<IActionResult> GetStockBatch([FromBody] IReadOnlyList<string> bookIds)
    {
        var result = new Dictionary<string, int>();
        foreach (var id in bookIds)
            result[id] = await _inventory.GetStockAsync(id);
        return Ok(result);
    }
}
