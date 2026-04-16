using System.Threading.Tasks;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BookingService.Consumers;

public sealed class AddToCartFailedConsumer : IConsumer<AddToCartFailed>
{
    private readonly ILogger<AddToCartFailedConsumer> _logger;

    public AddToCartFailedConsumer(ILogger<AddToCartFailedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<AddToCartFailed> context)
    {
        _logger.LogWarning($"Add to cart failed for user {context.Message.UserId}, book {context.Message.BookId}: {context.Message.Reason}");
        return Task.CompletedTask;
    }
}

