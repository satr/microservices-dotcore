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
}

