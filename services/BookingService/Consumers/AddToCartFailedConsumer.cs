using System.Threading.Tasks;
using BookingService.Repositories;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BookingService.Consumers;

public sealed class AddToCartFailedConsumer : IConsumer<AddToCartFailed>
{
    private readonly ILogger<AddToCartFailedConsumer> _logger;
    private readonly ICartRepository _cartRepository;

    public AddToCartFailedConsumer(ILogger<AddToCartFailedConsumer> logger, ICartRepository cartRepository)
    {
        _logger = logger;
        _cartRepository = cartRepository;
    }

    public Task Consume(ConsumeContext<AddToCartFailed> context)
    {
        _logger.LogWarning($"Add to cart failed for user {context.Message.UserId}, book {context.Message.BookId}: {context.Message.Reason}");
        
        // Record the failure so the frontend can retrieve it
        _cartRepository.RecordFailure(
            context.Message.UserId,
            context.Message.BookId,
            context.Message.Title,
            context.Message.Author,
            context.Message.Reason);
        
        return Task.CompletedTask;
    }
}
