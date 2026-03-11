using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Core.Common;
using QuizSystem.Core.DTOs;
using QuizSystem.Core.Entities;
using QuizSystem.Core.Enums;
using QuizSystem.Core.Interfaces;
using QuizSystem.Infrastructure.Data;
using QuizSystem.Infrastructure.Identity;
using QuizSystem.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace QuizSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly QuizSystemDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        QuizSystemDbContext dbContext,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var role = string.IsNullOrWhiteSpace(request.Role) ? AppRoles.Student : request.Role.Trim();

        if (!AppRoles.All.Contains(role))
        {
            throw new AppException($"Invalid role '{role}'.", HttpStatusCode.BadRequest);
        }

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new AppException("Email already exists.", HttpStatusCode.Conflict);
        }

        await EnsureRoleExistsAsync(role);

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

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new AppException(string.Join("; ", roleResult.Errors.Select(x => x.Description)));
        }

        return await BuildAuthResponseAsync(user, null, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.SingleOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (user is null)
        {
            throw new AppException("Invalid credentials.", HttpStatusCode.Unauthorized);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new AppException("User is locked.", HttpStatusCode.Forbidden);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            throw new AppException("Invalid credentials.", HttpStatusCode.Unauthorized);
        }

        return await BuildAuthResponseAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var userId = _tokenService.GetUserIdFromExpiredToken(request.AccessToken)
            ?? throw new AppException("Invalid access token.", HttpStatusCode.Unauthorized);

        var tokenRecord = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.Token == request.RefreshToken && x.UserId == userId, cancellationToken);

        if (tokenRecord is null || !tokenRecord.IsActive)
        {
            throw new AppException("Refresh token is invalid or expired.", HttpStatusCode.Unauthorized);
        }

        tokenRecord.RevokedAtUtc = DateTime.UtcNow;
        tokenRecord.ReplacedByToken = _tokenService.CreateRefreshToken();

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new AppException("User not found.", HttpStatusCode.NotFound);

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = await _tokenService.CreateAccessTokenAsync(user, roles);

        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = tokenRecord.ReplacedByToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        _dbContext.RefreshTokens.Update(tokenRecord);
        await _dbContext.RefreshTokens.AddAsync(replacement, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = replacement.Token,
            AccessTokenExpiresAtUtc = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                IsLocked = await _userManager.IsLockedOutAsync(user),
                Roles = roles
            }
        };
    }

    public async Task LogoutAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenRecord = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.Token == refreshToken, cancellationToken);

        if (tokenRecord is null || tokenRecord.RevokedAtUtc.HasValue)
        {
            return;
        }

        tokenRecord.RevokedAtUtc = DateTime.UtcNow;
        tokenRecord.CreatedByIp = ipAddress;

        _dbContext.RefreshTokens.Update(tokenRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user, string? ipAddress, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = await _tokenService.CreateAccessTokenAsync(user, roles);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _tokenService.CreateRefreshToken(),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAtUtc = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                IsLocked = await _userManager.IsLockedOutAsync(user),
                Roles = roles
            }
        };
    }

    private async Task EnsureRoleExistsAsync(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}
