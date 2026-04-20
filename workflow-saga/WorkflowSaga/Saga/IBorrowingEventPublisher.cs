using Library.Contracts.Messages;
using MassTransit;

namespace WorkflowSaga.Saga;

/// <summary>
/// Publishes borrowing outcome events.
/// Swapped per transport so state machine logic stays unchanged.
/// </summary>
public interface IBorrowingEventPublisher
{
    Task PublishCartItemAdded(CartItemAdded message, CancellationToken ct = default);
    Task PublishCartItemRemoved(CartItemRemoved message, CancellationToken ct = default);
    Task PublishBorrowingCompleted(BorrowingCompleted message, CancellationToken ct = default);
    Task PublishAddToCartFailed(AddToCartFailed message, CancellationToken ct = default);
}

/// <summary>RabbitMQ: uses IPublishEndpoint registered by the MassTransit bus.</summary>
public sealed class RabbitMqBorrowingEventPublisher : IBorrowingEventPublisher
{
    private readonly IPublishEndpoint _bus;
    public RabbitMqBorrowingEventPublisher(IPublishEndpoint bus) => _bus = bus;

    public Task PublishCartItemAdded(CartItemAdded m, CancellationToken ct = default) => _bus.Publish(m, ct);
    public Task PublishCartItemRemoved(CartItemRemoved m, CancellationToken ct = default) => _bus.Publish(m, ct);
    public Task PublishBorrowingCompleted(BorrowingCompleted m, CancellationToken ct = default) => _bus.Publish(m, ct);
    public Task PublishAddToCartFailed(AddToCartFailed m, CancellationToken ct = default) => _bus.Publish(m, ct);
}

/// <summary>Kafka: sends directly to topic producers registered by the Kafka Rider.</summary>
public sealed class KafkaBorrowingEventPublisher : IBorrowingEventPublisher
{
    private readonly ITopicProducer<CartItemAdded> _added;
    private readonly ITopicProducer<CartItemRemoved> _removed;
    private readonly ITopicProducer<BorrowingCompleted> _completed;
    private readonly ITopicProducer<AddToCartFailed> _failed;

    public KafkaBorrowingEventPublisher(
        ITopicProducer<CartItemAdded> added,
        ITopicProducer<CartItemRemoved> removed,
        ITopicProducer<BorrowingCompleted> completed,
        ITopicProducer<AddToCartFailed> failed)
    {
        _added = added;
        _removed = removed;
        _completed = completed;
        _failed = failed;
    }

    public Task PublishCartItemAdded(CartItemAdded m, CancellationToken ct = default) => _added.Produce(m, ct);
    public Task PublishCartItemRemoved(CartItemRemoved m, CancellationToken ct = default) => _removed.Produce(m, ct);
    public Task PublishBorrowingCompleted(BorrowingCompleted m, CancellationToken ct = default) => _completed.Produce(m, ct);
    public Task PublishAddToCartFailed(AddToCartFailed m, CancellationToken ct = default) => _failed.Produce(m, ct);
}

