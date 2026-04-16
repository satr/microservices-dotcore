using BookingService.Models;
using System.Collections.Concurrent;

namespace BookingService.Repositories;

public sealed class InMemoryCartRepository : ICartRepository
{
    private readonly ConcurrentDictionary<string, List<CartItem>> _cartByUser = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<CartItem> GetByUser(string userId)
    {
        if (!_cartByUser.TryGetValue(userId, out var items))
        {
            return [];
        }

        lock (items)
        {
            return items.ToArray();
        }
    }

    public void Add(string userId, CartItem item)
    {
        var items = _cartByUser.GetOrAdd(userId, _ => []);
        lock (items)
        {
            items.Add(item);
        }
    }

    public void Remove(string userId, string bookId)
    {
        if (!_cartByUser.TryGetValue(userId, out var items))
        {
            return;
        }

        lock (items)
        {
            var found = items.FirstOrDefault(i => string.Equals(i.BookId, bookId, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                items.Remove(found);
            }
        }
    }

    public void Clear(string userId)
    {
        if (!_cartByUser.TryGetValue(userId, out var items))
        {
            return;
        }

        lock (items)
        {
            items.Clear();
        }
    }
}

