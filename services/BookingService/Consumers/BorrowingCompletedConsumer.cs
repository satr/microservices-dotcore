using BookingService.Repositories;
using Library.Contracts.Messages;
using MassTransit;

namespace BookingService.Consumers;

public sealed class BorrowingCompletedConsumer : IConsumer<BorrowingCompleted>
{
    private readonly ICartRepository _carts;

    public BorrowingCompletedConsumer(ICartRepository carts)
    {
        _carts = carts;
    }

    public Task Consume(ConsumeContext<BorrowingCompleted> context)
    {
        _carts.Clear(context.Message.UserId);
        return Task.CompletedTask;
    }
}

