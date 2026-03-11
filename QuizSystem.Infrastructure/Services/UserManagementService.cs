using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Core.Common;
using QuizSystem.Core.DTOs;
using QuizSystem.Core.Enums;
using QuizSystem.Core.Interfaces;
using QuizSystem.Infrastructure.Identity;

namespace QuizSystem.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UserManagementService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyCollection<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.OrderBy(x => x.Email).ToListAsync(cancellationToken);

        var dtos = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                IsLocked = await _userManager.IsLockedOutAsync(user),
                Roles = roles
            });
        }

        return dtos;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Roles.Count == 0)
        {
            throw new AppException("At least one role must be assigned.");
        }

        if (request.Roles.Any(x => !AppRoles.All.Contains(x)))
        {
            throw new AppException("One or more roles are invalid.");
        }

        var existing = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existing is not null)
        {
            throw new AppException("Email already exists.", HttpStatusCode.Conflict);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            UserName = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new AppException(string.Join("; ", createResult.Errors.Select(x => x.Description)));
        }

        foreach (var role in request.Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
        if (!roleResult.Succeeded)
        {
            throw new AppException(string.Join("; ", roleResult.Errors.Select(x => x.Description)));
        }

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            IsLocked = false,
            Roles = request.Roles
        };
    }

    public async Task LockUnlockUserAsync(Guid userId, bool lockUser, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new AppException("User not found.", HttpStatusCode.NotFound);

        var lockoutEnd = lockUser ? DateTimeOffset.UtcNow.AddYears(100) : DateTimeOffset.UtcNow;
        var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

        if (!result.Succeeded)
        {
            throw new AppException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
    }

    public async Task AssignRoleAsync(AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (!AppRoles.All.Contains(request.Role))
        {
            throw new AppException("Invalid role.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new AppException("User not found.", HttpStatusCode.NotFound);

        var existingRoles = await _userManager.GetRolesAsync(user);
        var remove = await _userManager.RemoveFromRolesAsync(user, existingRoles);
        if (!remove.Succeeded)
        {
            throw new AppException(string.Join("; ", remove.Errors.Select(x => x.Description)));
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(request.Role));
        }

        var addResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
        {
            throw new AppException(string.Join("; ", addResult.Errors.Select(x => x.Description)));
        }
    }

    public async Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles.OrderBy(x => x.Name).Select(x => x.Name!).ToListAsync(cancellationToken);
        return roles.Select(x => new RoleDto { Name = x }).ToList();
    }
}
