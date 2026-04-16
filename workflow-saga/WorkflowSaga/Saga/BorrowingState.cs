using MassTransit;

namespace WorkflowSaga.Saga;

public sealed class BorrowingState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? BookId { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
}

