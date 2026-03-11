using QuizSystem.Core.DTOs;

namespace QuizSystem.Core.Interfaces;

public interface IUserManagementService
{
    Task<IReadOnlyCollection<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task LockUnlockUserAsync(Guid userId, bool lockUser, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(AssignRoleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
}
