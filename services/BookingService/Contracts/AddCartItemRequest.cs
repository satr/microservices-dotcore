namespace BookingService.Contracts;

public sealed record AddCartItemRequest(string UserId, string BookId, string Title, string Author);

