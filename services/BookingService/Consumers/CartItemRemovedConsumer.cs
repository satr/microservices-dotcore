using BookingService.Repositories;
using BookingService.Services;
using Library.Contracts.Messages;
using MassTransit;

namespace BookingService.Consumers;

public sealed class CartItemRemovedConsumer : IConsumer<CartItemRemoved>
{
    private readonly ICartRepository _carts;
    private readonly IBookInventoryService _inventory;
    private readonly ILogger<CartItemRemovedConsumer> _logger;

    public CartItemRemovedConsumer(ILogger<CartItemRemovedConsumer> logger, ICartRepository carts, IBookInventoryService inventory)
    {
        _carts = carts;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CartItemRemoved> context)
    {
        var message = context.Message;
        _logger.LogInformation("Cart item removing for user {UserId}, book {BookId}", message.UserId, message.BookId);
        _carts.Remove(message.UserId, message.BookId);
        await _inventory.ReleaseBookAsync(message.BookId);
        _logger.LogInformation("Cart item removed and stock restored for user {UserId}, book {BookId}", message.UserId, message.BookId);
    }
}
