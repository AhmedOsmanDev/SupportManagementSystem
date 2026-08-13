using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMS.Application;
using SMS.Domain;

namespace SMS.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ManagedUserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ManagedUserDto>>> GetUsers(
        [FromQuery] UserRole? role,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default) =>
        Ok(await userService.GetUsersAsync(role, activeOnly, cancellationToken));

    [HttpPost]
    [ProducesResponseType<ManagedUserDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ManagedUserDto>> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateUserAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        await userService.SetActiveAsync(id, request, cancellationToken);
        return NoContent();
    }
}
