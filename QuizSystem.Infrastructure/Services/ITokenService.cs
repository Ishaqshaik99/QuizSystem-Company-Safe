using QuizSystem.Infrastructure.Identity;

namespace QuizSystem.Infrastructure.Services;

public interface ITokenService
{
    Task<(string token, DateTime expiresAtUtc)> CreateAccessTokenAsync(ApplicationUser user, IReadOnlyCollection<string> roles);
    string CreateRefreshToken();
    Guid? GetUserIdFromExpiredToken(string accessToken);
}
