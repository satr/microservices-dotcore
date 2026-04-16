namespace BookingService.Models;

public sealed record CartItemFailure(string BookId, string Title, string Author, string Reason, DateTime FailedAtUtc);

