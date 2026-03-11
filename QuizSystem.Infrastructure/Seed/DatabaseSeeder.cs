using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuizSystem.Core.Entities;
using QuizSystem.Core.Enums;
using QuizSystem.Infrastructure.Data;
using QuizSystem.Infrastructure.Identity;

namespace QuizSystem.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
        var dbContext = scope.ServiceProvider.GetRequiredService<QuizSystemDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        var hasMigrations = dbContext.Database.GetMigrations().Any();
        if (hasMigrations)
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        var adminPassword = ResolveSeedPassword("<SET_PASSWORD>");
        var instructorPassword = ResolveSeedPassword("<SET_PASSWORD>");
        var studentPassword = ResolveSeedPassword("<SET_PASSWORD>");

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var admin = await EnsureUserAsync(userManager, "admin@quizsystem.local", "System Admin", adminPassword, cancellationToken);
        var instructor = await EnsureUserAsync(userManager, "instructor@quizsystem.local", "Demo Instructor", instructorPassword, cancellationToken);
        var student1 = await EnsureUserAsync(userManager, "student1@quizsystem.local", "Demo Student One", studentPassword, cancellationToken);
        var student2 = await EnsureUserAsync(userManager, "student2@quizsystem.local", "Demo Student Two", studentPassword, cancellationToken);

        await EnsureRolesAsync(userManager, admin, AppRoles.Admin);
        await EnsureRolesAsync(userManager, instructor, AppRoles.Instructor);
        await EnsureRolesAsync(userManager, student1, AppRoles.Student);
        await EnsureRolesAsync(userManager, student2, AppRoles.Student);

        if (!await dbContext.Topics.AnyAsync(cancellationToken))
        {
            var topic1 = new Topic { Id = Guid.NewGuid(), Name = "C# Fundamentals" };
            var topic2 = new Topic { Id = Guid.NewGuid(), Name = "Databases" };
            await dbContext.Topics.AddRangeAsync(new[] { topic1, topic2 }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Database seed completed.");
    }

    private static string ResolveSeedPassword(string configuredPassword)
    {
        if (string.IsNullOrWhiteSpace(configuredPassword) || configuredPassword.Contains("<SET_PASSWORD>", StringComparison.OrdinalIgnoreCase))
        {
            return "Passw0rd@123";
        }

        return configuredPassword;
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return user;
        }

        var created = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(created, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        return created;
    }

    private static async Task EnsureRolesAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string role)
    {
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
