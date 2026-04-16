using BookingService.Models;
using System.Collections.Concurrent;

namespace BookingService.Repositories;

public sealed class InMemoryCartRepository : ICartRepository
{
    private readonly ConcurrentDictionary<string, List<CartItem>> _cartByUser = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<CartItemFailure>> _failuresByUser = new(StringComparer.OrdinalIgnoreCase);

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

    public void RecordFailure(string userId, string bookId, string title, string author, string reason)
    {
        var failures = _failuresByUser.GetOrAdd(userId, _ => []);
        lock (failures)
        {
            // Remove any existing failure for this book
            var existing = failures.FirstOrDefault(f => string.Equals(f.BookId, bookId, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                failures.Remove(existing);
            }
            // Add new failure
            failures.Add(new CartItemFailure(bookId, title, author, reason, DateTime.UtcNow));
        }
    }

    public IReadOnlyList<CartItemFailure> GetFailuresByUser(string userId)
    {
        if (!_failuresByUser.TryGetValue(userId, out var failures))
        {
            return [];
        }

        lock (failures)
        {
            return failures.ToArray();
        }
    }

    public void ClearFailure(string userId, string bookId)
    {
        if (!_failuresByUser.TryGetValue(userId, out var failures))
        {
            return;
        }

        lock (failures)
        {
            var found = failures.FirstOrDefault(f => string.Equals(f.BookId, bookId, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                failures.Remove(found);
            }
        }
    }
}

