using BookingService.Data;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Repositories;

public sealed class PostgresCartRepository : ICartRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresCartRepository(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public IReadOnlyList<CartItem> GetByUser(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        return db.CartItems.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.AddedAtUtc)
            .Select(c => new CartItem(c.BookId, c.Title, c.Author))
            .ToArray();
    }

    public void Add(string userId, CartItem item)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        db.CartItems.Add(new CartItemEntity
        {
            UserId = userId,
            BookId = item.BookId,
            Title = item.Title,
            Author = item.Author,
            AddedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    public void Remove(string userId, string bookId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var entity = db.CartItems
            .FirstOrDefault(c => c.UserId == userId && c.BookId == bookId);
        if (entity is not null)
        {
            db.CartItems.Remove(entity);
            db.SaveChanges();
        }
    }

    public void Clear(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var items = db.CartItems.Where(c => c.UserId == userId).ToList();
        db.CartItems.RemoveRange(items);
        db.SaveChanges();
    }

    public void RecordFailure(string userId, string bookId, string title, string author, string reason)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        // Remove any existing failure for this book
        var existing = db.CartItemFailures
            .FirstOrDefault(f => f.UserId == userId && f.BookId == bookId);
        if (existing is not null)
        {
            db.CartItemFailures.Remove(existing);
        }
        // Add new failure
        db.CartItemFailures.Add(new CartItemFailureEntity
        {
            UserId = userId,
            BookId = bookId,
            Title = title,
            Author = author,
            Reason = reason,
            FailedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    public IReadOnlyList<CartItemFailure> GetFailuresByUser(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        return db.CartItemFailures.AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.FailedAtUtc)
            .Select(f => new CartItemFailure(f.BookId, f.Title, f.Author, f.Reason, f.FailedAtUtc))
            .ToArray();
    }

    public void ClearFailure(string userId, string bookId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var entity = db.CartItemFailures
            .FirstOrDefault(f => f.UserId == userId && f.BookId == bookId);
        if (entity is not null)
        {
            db.CartItemFailures.Remove(entity);
            db.SaveChanges();
        }
    }
}

