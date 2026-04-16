using UsersService.Models;

namespace UsersService.Repositories;

public sealed class InMemoryUserRepository : IUserRepository
{
    private static readonly UserRecord[] Users =
    [
        new("u1", "user1"),
        new("u2", "user2")
    ];

    public UserRecord? FindByUserName(string userName)
    {
        return Users.FirstOrDefault(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));
    }
}

