namespace BookingService.Data;

public sealed class CartItemFailureEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string BookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime FailedAtUtc { get; set; } = DateTime.UtcNow;
}

