using System.Threading.Tasks;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BookingService.Consumers;

public sealed class CartItemRemovalConfirmedConsumer : IConsumer<CartItemRemovalConfirmed>
{
    private readonly ILogger<CartItemRemovalConfirmedConsumer> _logger;

    public CartItemRemovalConfirmedConsumer(ILogger<CartItemRemovalConfirmedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<CartItemRemovalConfirmed> context)
    {
        _logger.LogInformation($"Cart item removal confirmed for user {context.Message.UserId}, book {context.Message.BookId}");
        return Task.CompletedTask;
    }
}

