using System;

namespace Library.Contracts.Messages;

public sealed record AddToCartRequested(
    Guid CorrelationId,
    string UserId,
    string BookId,
    string Title,
    string Author,
    DateTime RequestedAtUtc);

public sealed record RemoveFromCartRequested(
    Guid CorrelationId,
    string UserId,
    string BookId,
    DateTime RequestedAtUtc);

public sealed record CompleteBorrowingRequested(
    Guid CorrelationId,
    string UserId,
    DateTime RequestedAtUtc);

public sealed record CartItemAdded(
    Guid CorrelationId,
    string UserId,
    string BookId,
    string Title,
    string Author);

public sealed record CartItemRemoved(
    Guid CorrelationId,
    string UserId,
    string BookId);

public sealed record BorrowingCompleted(
    Guid CorrelationId,
    string UserId);

// Failure events
public sealed record AddToCartFailed(
    Guid CorrelationId,
    string UserId,
    string BookId,
    string Reason);

public sealed record CartItemRemovalConfirmed(
    Guid CorrelationId,
    string UserId,
    string BookId);

public sealed record BookStockRestored(
    Guid CorrelationId,
    string BookId,
    int QuantityRestored);

