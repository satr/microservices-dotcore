using BookingService.Models;

namespace BookingService.Repositories;

// Placeholder for future PostgreSQL implementation.
public sealed class PostgresCartRepository : ICartRepository
{
    public IReadOnlyList<CartItem> GetByUser(string userId) => [];

    public void Add(string userId, CartItem item)
    {
    }

    public void Remove(string userId, string bookId)
    {
    }

    public void Clear(string userId)
    {
    }
}

