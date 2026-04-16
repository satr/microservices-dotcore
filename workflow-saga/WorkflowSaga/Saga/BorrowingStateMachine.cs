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
                    ctx => ctx.Message.BookId != "b2", // Simulate b2 being out of stock initially
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
                            "Book is currently out of stock"))
                        .TransitionTo(Failed)
                        .Finalize()
                ),

            When(RemoveFromCartRequestedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.BookId = ctx.Message.BookId;
                })
                .Publish(ctx => new CartItemRemovalConfirmed(
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
}



