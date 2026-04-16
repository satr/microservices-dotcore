using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UsersService.Data;

/// <summary>Used only by EF Core tooling (dotnet ef migrations).</summary>
public sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql("Host=localhost;Database=users_db;Username=library;Password=library")
            .Options;
        return new UsersDbContext(options);
    }
}

