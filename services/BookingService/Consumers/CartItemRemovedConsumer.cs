using BookingService.Repositories;
using Library.Contracts.Messages;
using MassTransit;

namespace BookingService.Consumers;

public sealed class CartItemRemovedConsumer : IConsumer<CartItemRemoved>
{
    private readonly ICartRepository _carts;
    private readonly ILogger<CartItemRemovedConsumer> _logger;

    public CartItemRemovedConsumer(ILogger<CartItemRemovedConsumer> logger, ICartRepository carts)
    {
        _carts = carts;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<CartItemRemoved> context)
    {
        _logger.LogInformation($"Cart item removing for user {context.Message.UserId}, book {context.Message.BookId}");
        var message = context.Message;
        _carts.Remove(message.UserId, message.BookId);
        _logger.LogInformation($"Cart item removed for user {context.Message.UserId}, book {context.Message.BookId}");
        return Task.CompletedTask;
    }
}

