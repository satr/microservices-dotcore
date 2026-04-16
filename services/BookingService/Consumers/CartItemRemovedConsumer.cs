using BookingService.Repositories;
using Library.Contracts.Messages;
using MassTransit;

namespace BookingService.Consumers;

public sealed class CartItemRemovedConsumer : IConsumer<CartItemRemoved>
{
    private readonly ICartRepository _carts;

    public CartItemRemovedConsumer(ICartRepository carts)
    {
        _carts = carts;
    }

    public Task Consume(ConsumeContext<CartItemRemoved> context)
    {
        var message = context.Message;
        _carts.Remove(message.UserId, message.BookId);
        return Task.CompletedTask;
    }
}

