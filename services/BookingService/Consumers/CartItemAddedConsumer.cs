using BookingService.Models;
using BookingService.Repositories;
using BookingService.Services;
using Library.Contracts.Messages;
using MassTransit;

namespace BookingService.Consumers;

public sealed class CartItemAddedConsumer : IConsumer<CartItemAdded>
{
    private readonly ILogger<CartItemAddedConsumer> _logger;
    private readonly ICartRepository _carts;
    private readonly IBookInventoryService _inventory;
    private readonly IPublishEndpoint _publish;

    public CartItemAddedConsumer(
        ILogger<CartItemAddedConsumer> logger,
        ICartRepository carts,
        IBookInventoryService inventory,
        IPublishEndpoint publish)
    {
        _logger = logger;
        _carts = carts;
        _inventory = inventory;
        _publish = publish;
    }

    public async Task Consume(ConsumeContext<CartItemAdded> context)
    {
        var message = context.Message;
        var deducted = await _inventory.DeductStockAsync(message.BookId);

        if (!deducted)
        {
            _logger.LogWarning("Book {BookId} out of stock — rejecting cart add for user {UserId}", message.BookId, message.UserId);
            await _publish.Publish(new AddToCartFailed(
                message.CorrelationId,
                message.UserId,
                message.BookId,
                "Book is out of stock",
                message.Title,
                message.Author));
            return;
        }

        _carts.Add(message.UserId, new CartItem(message.BookId, message.Title, message.Author));
        _logger.LogInformation("Cart item added for user {UserId}, book {BookId}", message.UserId, message.BookId);
    }
}

