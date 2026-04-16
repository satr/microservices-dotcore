using BookingService.Models;
using BookingService.Repositories;
using Library.Contracts.Messages;
using MassTransit;

namespace BookingService.Consumers;

public sealed class CartItemAddedConsumer : IConsumer<CartItemAdded>
{
    private readonly ICartRepository _carts;

    public CartItemAddedConsumer(ICartRepository carts)
    {
        _carts = carts;
    }

    public Task Consume(ConsumeContext<CartItemAdded> context)
    {
        var message = context.Message;
        _carts.Add(message.UserId, new CartItem(message.BookId, message.Title, message.Author));
        return Task.CompletedTask;
    }
}

