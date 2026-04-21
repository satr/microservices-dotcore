
namespace BookingService.Data;

/// <summary>Tracks book inventory for the booking service (stock availability).</summary>
public sealed class BookInventoryEntity
{
    public string BookId { get; set; } = string.Empty;
    public int Stock { get; set; } = 10;
}


