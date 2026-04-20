using Library.Contracts.Messages;
using MassTransit;

namespace WorkflowSaga.Saga;

public sealed class BorrowingStateMachine : MassTransitStateMachine<BorrowingState>
{
    public State Requested { get; private set; } = null!;
    public State ItemRemoved { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<AddToCartRequested> AddToCartRequestedEvent { get; private set; } = null!;
    public Event<RemoveFromCartRequested> RemoveFromCartRequestedEvent { get; private set; } = null!;
    public Event<CompleteBorrowingRequested> CompleteBorrowingRequestedEvent { get; private set; } = null!;

    public BorrowingStateMachine()
    {
        InstanceState(state => state.CurrentState);

        Event(() => AddToCartRequestedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => RemoveFromCartRequestedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
        Event(() => CompleteBorrowingRequestedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));

        Initially(
            When(AddToCartRequestedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.BookId = ctx.Message.BookId;
                    ctx.Saga.Title = ctx.Message.Title;
                    ctx.Saga.Author = ctx.Message.Author;
                })
                .IfElse(
                    ctx => IsBookInStock(ctx.Message.BookId),
                    ifTrue => ifTrue
                        .Publish(ctx => new CartItemAdded(
                            ctx.Saga.CorrelationId,
                            ctx.Message.UserId,
                            ctx.Message.BookId,
                            ctx.Message.Title,
                            ctx.Message.Author))
                        .TransitionTo(Requested)
                        .Finalize(),
                    ifFalse => ifFalse
                        .Publish(ctx => new AddToCartFailed(
                            ctx.Saga.CorrelationId,
                            ctx.Message.UserId,
                            ctx.Message.BookId,
                            "Book is currently out of stock or inventory service unavailable"))
                        .TransitionTo(Failed)
                        .Finalize()
                ),

            When(RemoveFromCartRequestedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.BookId = ctx.Message.BookId;
                })
                .Publish(ctx => new CartItemRemoved(
                    ctx.Saga.CorrelationId,
                    ctx.Message.UserId,
                    ctx.Message.BookId))
                .TransitionTo(ItemRemoved)
                .Finalize(),

            When(CompleteBorrowingRequestedEvent)
                .Then(ctx => ctx.Saga.UserId = ctx.Message.UserId)
                .Publish(ctx => new BorrowingCompleted(
                    ctx.Saga.CorrelationId,
                    ctx.Message.UserId))
                .TransitionTo(Requested)
                .Finalize());

        SetCompletedWhenFinalized();
    }

    /// <summary>
    /// Simulate stock check with 50% random failure rate using book ID hash.
    /// This demonstrates the inventory service's unreliability pattern.
    /// </summary>
    private static bool IsBookInStock(string bookId)
    {
        return true;
        // Use book ID hash to seed pseudo-random decision (50/50 chance of success)
        // var hash = bookId.GetHashCode();
        // return (Math.Abs(hash) % 2) == 0;
    }
}



