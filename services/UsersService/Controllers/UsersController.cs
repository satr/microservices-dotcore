using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using UsersService.Repositories;

namespace UsersService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserRepository _users;

    public UsersController(IUserRepository users)
    {
        _users = users;
    }

    [HttpGet("by-name/{userName}")]
    public IActionResult GetByUserName(string userName)
    {
        var user = _users.FindByUserName(userName);
        return user is null ? NotFound() : Ok(user);
    }
}

