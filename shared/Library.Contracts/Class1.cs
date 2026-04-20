using System;

namespace Library.Contracts.Messages;

// ---------------------------------------------------------------------------
// Message envelope — carries schema version alongside any payload.
// Consumers check SchemaVersion before deserialising Payload so they can
// handle or reject unknown versions gracefully without crashing.
// ---------------------------------------------------------------------------

/// <summary>
/// Transport-level envelope that wraps a message payload with schema metadata.
/// Use this when publishing to Kafka topics so downstream consumers can detect
/// and handle version mismatches.
/// </summary>
/// <param name="SchemaVersion">Monotonically increasing integer. Increment on breaking changes.</param>
/// <param name="MessageType">Message type name, e.g. "AddToCartRequested".</param>
/// <param name="Payload">The actual message payload.</param>
public sealed record MessageEnvelope<T>(
    int SchemaVersion,
    string MessageType,
    T Payload)
{
    /// <summary>Wrap a payload at version 1 (initial schema).</summary>
    public static MessageEnvelope<T> V1(T payload) =>
        new(1, typeof(T).Name, payload);
}


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

public sealed record BookStockRestored(
    Guid CorrelationId,
    string BookId,
    int QuantityRestored);

