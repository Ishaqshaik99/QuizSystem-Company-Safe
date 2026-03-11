using Microsoft.AspNetCore.Identity;

namespace QuizSystem.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
}
