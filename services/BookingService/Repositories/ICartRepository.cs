using BookingService.Models;

namespace BookingService.Repositories;

public interface ICartRepository
{
    IReadOnlyList<CartItem> GetByUser(string userId);
    void Add(string userId, CartItem item);
    void Remove(string userId, string bookId);
    void Clear(string userId);
}

