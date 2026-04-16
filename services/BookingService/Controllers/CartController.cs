using BookingService.Contracts;
using BookingService.Repositories;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[Route("api/cart")]
public sealed class CartController : ControllerBase
{
    private readonly ICartRepository _carts;
    private readonly IPublishEndpoint _publish;

    public CartController(ICartRepository carts, IPublishEndpoint publish)
    {
        _carts = carts;
        _publish = publish;
    }

    [HttpGet("{userId}")]
    public IActionResult GetCart(string userId)
    {
        return Ok(_carts.GetByUser(userId));
    }

    [HttpPost("items")]
    public async Task<IActionResult> Add([FromBody] AddCartItemRequest request)
    {
        await _publish.Publish(new AddToCartRequested(
            Guid.NewGuid(),
            request.UserId,
            request.BookId,
            request.Title,
            request.Author,
            DateTime.UtcNow));

        return Accepted();
    }

    [HttpDelete("items/{bookId}")]
    public async Task<IActionResult> Remove(string bookId, [FromQuery] string userId)
    {
        await _publish.Publish(new RemoveFromCartRequested(
            Guid.NewGuid(),
            userId,
            bookId,
            DateTime.UtcNow));

        return Accepted();
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromQuery] string userId)
    {
        await _publish.Publish(new CompleteBorrowingRequested(
            Guid.NewGuid(),
            userId,
            DateTime.UtcNow));

        return Accepted();
    }

    // ReSharper disable once UnusedMember.Global
    [HttpGet("failures/{userId}")]
    public IActionResult GetFailures(string userId)
    {
        return Ok(_carts.GetFailuresByUser(userId));
    }

    [HttpDelete("failures/{userId}/{bookId}")]
    public IActionResult ClearFailure(string userId, string bookId)
    {
        _carts.ClearFailure(userId, bookId);
        return NoContent();
    }
}

