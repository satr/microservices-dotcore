using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BooksService.Data;

/// <summary>Used only by EF Core tooling (dotnet ef migrations).</summary>
public sealed class BooksDbContextFactory : IDesignTimeDbContextFactory<BooksDbContext>
{
    public BooksDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BooksDbContext>()
            .UseNpgsql("Host=localhost;Database=books_db;Username=library;Password=library")
            .Options;
        return new BooksDbContext(options);
    }
}

