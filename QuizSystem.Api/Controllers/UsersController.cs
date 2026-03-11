using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Core.DTOs;
using QuizSystem.Core.Enums;
using QuizSystem.Core.Interfaces;

namespace QuizSystem.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await _userManagementService.GetUsersAsync(cancellationToken));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyCollection<RoleDto>>> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await _userManagementService.GetRolesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _userManagementService.CreateUserAsync(request, cancellationToken);
        return Ok(created);
    }

    [HttpPut("{userId:guid}/lock")]
    public async Task<IActionResult> LockUser(Guid userId, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        await _userManagementService.LockUnlockUserAsync(userId, request.LockUser, cancellationToken);
        return NoContent();
    }

    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        await _userManagementService.AssignRoleAsync(request, cancellationToken);
        return NoContent();
    }
}
