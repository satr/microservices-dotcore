using BookingService.Models;

namespace BookingService.Repositories;

public interface ICartRepository
{
    IReadOnlyList<CartItem> GetByUser(string userId);
    void Add(string userId, CartItem item);
    void Remove(string userId, string bookId);
    void Clear(string userId);
    void RecordFailure(string userId, string bookId, string title, string author, string reason);
    IReadOnlyList<CartItemFailure> GetFailuresByUser(string userId);
    void ClearFailure(string userId, string bookId);
}

