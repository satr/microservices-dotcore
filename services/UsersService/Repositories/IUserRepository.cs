using UsersService.Models;

namespace UsersService.Repositories;

public interface IUserRepository
{
    UserRecord? FindByUserName(string userName);
}

