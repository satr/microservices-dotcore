using System;
using System.Threading.Tasks;
using BookingService.Data;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Services;

public interface IBookInventoryService
{
    /// <summary>Check if book is in stock and reserve it.</summary>
    Task<(bool Success, string Message)> ReserveBookAsync(string bookId);
    
    /// <summary>Release reserved stock (e.g., when item removed from cart).</summary>
    Task ReleaseBookAsync(string bookId);
    
    /// <summary>Deduct stock after successful borrowing completion.</summary>
    Task<bool> DeductStockAsync(string bookId);
    
    Task<int> GetStockAsync(string bookId);
}

public sealed class BookInventoryService : IBookInventoryService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Random _random = new();

    public BookInventoryService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<(bool Success, string Message)> ReserveBookAsync(string bookId)
    {
        // Simulate 50% failure rate to demonstrate resilience
        if (_random.Next(0, 2) == 0)
        {
            return (false, $"Temporary inventory service issue for book {bookId}");
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        
        var inventory = await db.BookInventories.FindAsync(bookId);
        if (inventory == null || inventory.Stock <= 0)
        {
            return (false, $"Book {bookId} is out of stock");
        }

        // Don't actually deduct here; just reserve.
        return (true, "Book reserved");
    }

    public async Task ReleaseBookAsync(string bookId)
    {
        // Stock is released when item is removed from cart
        // In a real system, this would involve tracking reserved quantities
        await Task.CompletedTask;
    }

    public async Task<bool> DeductStockAsync(string bookId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        
        var inventory = await db.BookInventories.FindAsync(bookId);
        if (inventory == null || inventory.Stock <= 0)
        {
            return false;
        }

        inventory.Stock--;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetStockAsync(string bookId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        
        var inventory = await db.BookInventories.FindAsync(bookId);
        return inventory?.Stock ?? 0;
    }
}

