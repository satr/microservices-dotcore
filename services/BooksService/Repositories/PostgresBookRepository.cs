using BooksService.Models;
using Microsoft.EntityFrameworkCore;

namespace BooksService.Repositories;

public sealed class PostgresBookRepository : IBookRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresBookRepository(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public IReadOnlyList<BookRecord> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.BooksDbContext>();
        var lower = query.ToLower();
        return db.Books.AsNoTracking()
            .Where(b => b.Title.ToLower().Contains(lower))
            .ToArray();
    }

    public BookRecord? GetById(string id)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.BooksDbContext>();
        return db.Books.AsNoTracking()
            .FirstOrDefault(b => b.Id == id);
    }
}

