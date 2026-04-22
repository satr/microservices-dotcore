using Asp.Versioning;
using BookingService.Contracts;
using BookingService.Messaging;
using BookingService.Repositories;
using Library.Contracts.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cart")]
[Authorize] // all cart endpoints require authentication
public sealed class CartController : ControllerBase
{
    private readonly ICartRepository _carts;
    private readonly ICartCommandPublisher _publish;

    public CartController(ICartRepository carts, ICartCommandPublisher publish)
    {
        _carts = carts;
        _publish = publish;
    }

    [HttpGet("{userId}")]
    [Authorize(Roles = "member,librarian")]
    public IActionResult GetCart(string userId)
    {
        return Ok(_carts.GetByUser(userId));
    }

    [HttpPost("items")]
    [Authorize(Roles = "member")]
    public async Task<IActionResult> Add([FromBody] AddCartItemRequest request)
    {
        await _publish.PublishAddToCartRequested(new AddToCartRequested(
            Guid.NewGuid(),
            request.UserId,
            request.BookId,
            request.Title,
            request.Author,
            DateTime.UtcNow));

        return Accepted();
    }

    [HttpDelete("items/{bookId}")]
    [Authorize(Roles = "member")]
    public async Task<IActionResult> Remove(string bookId, [FromQuery] string userId)
    {
        await _publish.PublishRemoveFromCartRequested(new RemoveFromCartRequested(
            Guid.NewGuid(),
            userId,
            bookId,
            DateTime.UtcNow));

        return Accepted();
    }

    [HttpPost("complete")]
    [Authorize(Roles = "member")]
    public async Task<IActionResult> Complete([FromQuery] string userId)
    {
        await _publish.PublishCompleteBorrowingRequested(new CompleteBorrowingRequested(
            Guid.NewGuid(),
            userId,
            DateTime.UtcNow));

        return Accepted();
    }

    [HttpGet("failures/{userId}")]
    [Authorize(Roles = "member,librarian")]
    public IActionResult GetFailures(string userId)
    {
        return Ok(_carts.GetFailuresByUser(userId));
    }

    [HttpDelete("failures/{userId}/{bookId}")]
    [Authorize(Roles = "member,librarian")]
    public IActionResult ClearFailure(string userId, string bookId)
    {
        _carts.ClearFailure(userId, bookId);
        return NoContent();
    }
}
