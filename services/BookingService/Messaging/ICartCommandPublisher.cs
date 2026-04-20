using Library.Contracts.Messages;
using MassTransit;

namespace BookingService.Messaging;

/// <summary>
/// Transport-agnostic publisher for cart command messages.
/// In RabbitMQ mode wraps IPublishEndpoint.
/// In Kafka mode sends directly to topic producers.
/// </summary>
public interface ICartCommandPublisher
{
    Task PublishAddToCartRequested(AddToCartRequested message, CancellationToken ct = default);
    Task PublishRemoveFromCartRequested(RemoveFromCartRequested message, CancellationToken ct = default);
    Task PublishCompleteBorrowingRequested(CompleteBorrowingRequested message, CancellationToken ct = default);
}

/// <summary>RabbitMQ implementation — delegates to MassTransit IPublishEndpoint.</summary>
public sealed class RabbitMqCartCommandPublisher : ICartCommandPublisher
{
    private readonly IPublishEndpoint _publish;
    public RabbitMqCartCommandPublisher(IPublishEndpoint publish) => _publish = publish;

    public Task PublishAddToCartRequested(AddToCartRequested m, CancellationToken ct = default) => _publish.Publish(m, ct);
    public Task PublishRemoveFromCartRequested(RemoveFromCartRequested m, CancellationToken ct = default) => _publish.Publish(m, ct);
    public Task PublishCompleteBorrowingRequested(CompleteBorrowingRequested m, CancellationToken ct = default) => _publish.Publish(m, ct);
}

/// <summary>Kafka implementation — sends to per-message topics via ITopicProducer.</summary>
public sealed class KafkaCartCommandPublisher : ICartCommandPublisher
{
    private readonly ITopicProducer<AddToCartRequested> _addProducer;
    private readonly ITopicProducer<RemoveFromCartRequested> _removeProducer;
    private readonly ITopicProducer<CompleteBorrowingRequested> _completeProducer;

    public KafkaCartCommandPublisher(
        ITopicProducer<AddToCartRequested> addProducer,
        ITopicProducer<RemoveFromCartRequested> removeProducer,
        ITopicProducer<CompleteBorrowingRequested> completeProducer)
    {
        _addProducer = addProducer;
        _removeProducer = removeProducer;
        _completeProducer = completeProducer;
    }

    public Task PublishAddToCartRequested(AddToCartRequested m, CancellationToken ct = default) =>
        _addProducer.Produce(m, ct);

    public Task PublishRemoveFromCartRequested(RemoveFromCartRequested m, CancellationToken ct = default) =>
        _removeProducer.Produce(m, ct);

    public Task PublishCompleteBorrowingRequested(CompleteBorrowingRequested m, CancellationToken ct = default) =>
        _completeProducer.Produce(m, ct);
}

