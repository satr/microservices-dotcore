using Microsoft.EntityFrameworkCore;
using UsersService.Models;

namespace UsersService.Repositories;

public sealed class PostgresUserRepository : IUserRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresUserRepository(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public UserRecord? FindByUserName(string userName)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.UsersDbContext>();
        return db.Users.AsNoTracking()
            .FirstOrDefault(u => u.UserName.ToLower() == userName.ToLower());
    }
}

