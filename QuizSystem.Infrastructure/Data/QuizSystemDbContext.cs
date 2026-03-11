using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Core.Entities;
using QuizSystem.Infrastructure.Identity;

namespace QuizSystem.Infrastructure.Data;

public class QuizSystemDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public QuizSystemDbContext(DbContextOptions<QuizSystemDbContext> options)
        : base(options)
    {
    }

    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<GroupClass> GroupClasses => Set<GroupClass>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAssignment> QuizAssignments => Set<QuizAssignment>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Topic>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<GroupClass>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => new { x.InstructorId, x.Name }).IsUnique();
        });

        builder.Entity<GroupMembership>(entity =>
        {
            entity.HasOne(x => x.GroupClass)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.GroupClassId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.GroupClassId, x.StudentId }).IsUnique();
        });

        builder.Entity<Question>(entity =>
        {
            entity.Property(x => x.Stem).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ShortAnswerExpectedAnswer).HasMaxLength(1024);
            entity.HasOne(x => x.Topic)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.TopicId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => x.InstructorId);
            entity.HasIndex(x => x.TopicId);
        });

        builder.Entity<QuestionOption>(entity =>
        {
            entity.Property(x => x.Text).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.Question)
                .WithMany(x => x.Options)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.QuestionId, x.OrderIndex });
        });

        builder.Entity<Quiz>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasIndex(x => x.InstructorId);
            entity.HasIndex(x => x.IsPublished);
        });

        builder.Entity<QuizQuestion>(entity =>
        {
            entity.HasOne(x => x.Quiz)
                .WithMany(x => x.QuizQuestions)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Question)
                .WithMany(x => x.QuizQuestions)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.QuizId, x.QuestionId }).IsUnique();
        });

        builder.Entity<QuizAssignment>(entity =>
        {
            entity.HasOne(x => x.Quiz)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.GroupClass)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.GroupClassId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.QuizId, x.StudentId, x.GroupClassId }).IsUnique();
        });

        builder.Entity<Attempt>(entity =>
        {
            entity.HasOne(x => x.Quiz)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.StudentId, x.QuizId });
            entity.Property(x => x.Score).HasPrecision(10, 2);
            entity.Property(x => x.MaxScore).HasPrecision(10, 2);
            entity.Property(x => x.Percentage).HasPrecision(5, 2);
        });

        builder.Entity<AttemptAnswer>(entity =>
        {
            entity.HasOne(x => x.Attempt)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
            entity.Property(x => x.AwardedScore).HasPrecision(10, 2);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.Property(x => x.Token).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasIndex(x => x.UserId);
        });
    }
}
