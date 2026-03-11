using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuizSystem.Infrastructure.Data;

public class QuizSystemDbContextFactory : IDesignTimeDbContextFactory<QuizSystemDbContext>
{
    public QuizSystemDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QuizSystemDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=QuizSystemDb;Trusted_Connection=True;TrustServerCertificate=True;");

        return new QuizSystemDbContext(optionsBuilder.Options);
    }
}
