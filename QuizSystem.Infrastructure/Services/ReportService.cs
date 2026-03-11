using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Core.Common;
using QuizSystem.Core.DTOs;
using QuizSystem.Core.Enums;
using QuizSystem.Core.Interfaces;
using QuizSystem.Infrastructure.Data;

namespace QuizSystem.Infrastructure.Services;

public class ReportService : IReportService
{
    private static readonly (int min, int max)[] DistributionBins =
    {
        (0, 20),
        (21, 40),
        (41, 60),
        (61, 80),
        (81, 100)
    };

    private readonly QuizSystemDbContext _dbContext;

    public ReportService(QuizSystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StudentDashboardDto> GetStudentDashboardAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var attempts = await _dbContext.Attempts.AsNoTracking()
            .Where(x => x.StudentId == studentId && x.Status != AttemptStatus.InProgress)
            .OrderBy(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        var overall = attempts.Count == 0 ? 0 : Math.Round(attempts.Average(x => x.Percentage), 2, MidpointRounding.AwayFromZero);

        var attemptIds = attempts.Select(x => x.Id).ToList();
        var answers = await _dbContext.AttemptAnswers.AsNoTracking()
            .Where(x => attemptIds.Contains(x.AttemptId))
            .ToListAsync(cancellationToken);

        var questionLookup = await _dbContext.Questions.AsNoTracking()
            .Include(x => x.Topic)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var topicStats = answers
            .Where(x => questionLookup.ContainsKey(x.QuestionId) && x.IsCorrect.HasValue)
            .GroupBy(x => questionLookup[x.QuestionId].Topic?.Name ?? "Uncategorized")
            .Select(g => new TopicPerformanceDto
            {
                TopicName = g.Key,
                TotalQuestions = g.Count(),
                CorrectAnswers = g.Count(a => a.IsCorrect == true),
                Accuracy = g.Count() == 0 ? 0 : Math.Round((g.Count(a => a.IsCorrect == true) / (decimal)g.Count()) * 100m, 2)
            })
            .OrderByDescending(x => x.Accuracy)
            .ToList();

        var trend = attempts
            .Where(x => x.SubmittedAtUtc.HasValue)
            .Select(x => new TrendPointDto
            {
                AttemptDateUtc = x.SubmittedAtUtc!.Value,
                Percentage = x.Percentage
            })
            .ToList();

        return new StudentDashboardDto
        {
            OverallAccuracy = overall,
            TopicWise = topicStats,
            Trend = trend
        };
    }

    public async Task<InstructorQuizAnalyticsDto> GetQuizAnalyticsAsync(Guid requesterId, Guid quizId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var quiz = await _dbContext.Quizzes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == quizId, cancellationToken)
            ?? throw new AppException("Quiz not found.");

        if (!isAdmin && quiz.InstructorId != requesterId)
        {
            throw new AppException("Not allowed to view this quiz analytics.", System.Net.HttpStatusCode.Forbidden);
        }

        var attempts = await _dbContext.Attempts.AsNoTracking()
            .Where(x => x.QuizId == quizId && x.Status != AttemptStatus.InProgress)
            .ToListAsync(cancellationToken);

        var attemptIds = attempts.Select(x => x.Id).ToList();
        var answers = await _dbContext.AttemptAnswers.AsNoTracking()
            .Where(x => attemptIds.Contains(x.AttemptId))
            .ToListAsync(cancellationToken);

        var quizQuestions = await _dbContext.QuizQuestions.AsNoTracking()
            .Include(x => x.Question)
                .ThenInclude(q => q!.Topic)
            .Include(x => x.Question)
                .ThenInclude(q => q!.Options)
            .Where(x => x.QuizId == quizId)
            .ToListAsync(cancellationToken);

        var distribution = BuildDistribution(attempts);
        var questionPerformance = BuildQuestionPerformance(quizQuestions, answers);
        var topicPerformance = BuildTopicPerformance(quizQuestions, answers);

        return new InstructorQuizAnalyticsDto
        {
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            AttemptCount = attempts.Count,
            AverageScore = attempts.Count == 0 ? 0 : Math.Round(attempts.Average(x => x.Percentage), 2),
            MinScore = attempts.Count == 0 ? 0 : attempts.Min(x => x.Percentage),
            MaxScore = attempts.Count == 0 ? 0 : attempts.Max(x => x.Percentage),
            ScoreDistribution = distribution,
            QuestionPerformance = questionPerformance,
            TopicPerformance = topicPerformance
        };
    }

    public async Task<AdminOverviewDto> GetAdminOverviewAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        var totalQuizzes = await _dbContext.Quizzes.CountAsync(cancellationToken);
        var totalAttempts = await _dbContext.Attempts.CountAsync(cancellationToken);

        var avg = await _dbContext.Attempts
            .Where(x => x.Status != AttemptStatus.InProgress)
            .Select(x => (decimal?)x.Percentage)
            .AverageAsync(cancellationToken) ?? 0;

        return new AdminOverviewDto
        {
            TotalUsers = totalUsers,
            TotalQuizzes = totalQuizzes,
            TotalAttempts = totalAttempts,
            AverageScorePercentage = Math.Round(avg, 2)
        };
    }

    private static IReadOnlyCollection<ScoreDistributionBinDto> BuildDistribution(IReadOnlyCollection<QuizSystem.Core.Entities.Attempt> attempts)
    {
        return DistributionBins.Select(bin => new ScoreDistributionBinDto
        {
            RangeLabel = $"{bin.min}-{bin.max}",
            Count = attempts.Count(a => a.Percentage >= bin.min && a.Percentage <= bin.max)
        }).ToList();
    }

    private static IReadOnlyCollection<QuestionPerformanceDto> BuildQuestionPerformance(
        IReadOnlyCollection<QuizSystem.Core.Entities.QuizQuestion> quizQuestions,
        IReadOnlyCollection<QuizSystem.Core.Entities.AttemptAnswer> answers)
    {
        var list = new List<QuestionPerformanceDto>();

        foreach (var quizQuestion in quizQuestions)
        {
            var question = quizQuestion.Question;
            if (question is null)
            {
                continue;
            }

            var questionAnswers = answers.Where(x => x.QuestionId == question.Id).ToList();
            var correctRate = questionAnswers.Count == 0
                ? 0
                : Math.Round((questionAnswers.Count(x => x.IsCorrect == true) / (decimal)questionAnswers.Count) * 100m, 2);

            var optionLookup = question.Options.ToDictionary(x => x.Id, x => x.Text);
            var wrongOptions = questionAnswers
                .Where(x => x.IsCorrect == false && !string.IsNullOrWhiteSpace(x.SelectedOptionIdsJson))
                .SelectMany(x => DeserializeOptionIds(x.SelectedOptionIdsJson!))
                .GroupBy(x => optionLookup.TryGetValue(x, out var text) ? text : "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            list.Add(new QuestionPerformanceDto
            {
                QuestionId = question.Id,
                Stem = question.Stem,
                CorrectRate = correctRate,
                CommonWrongOptions = wrongOptions
            });
        }

        return list;
    }

    private static IReadOnlyCollection<TopicPerformanceDto> BuildTopicPerformance(
        IReadOnlyCollection<QuizSystem.Core.Entities.QuizQuestion> quizQuestions,
        IReadOnlyCollection<QuizSystem.Core.Entities.AttemptAnswer> answers)
    {
        return quizQuestions
            .Where(x => x.Question is not null)
            .GroupBy(x => x.Question!.Topic?.Name ?? "Uncategorized")
            .Select(group =>
            {
                var ids = group.Select(x => x.QuestionId).ToHashSet();
                var topicAnswers = answers.Where(x => ids.Contains(x.QuestionId) && x.IsCorrect.HasValue).ToList();
                var total = topicAnswers.Count;
                var correct = topicAnswers.Count(x => x.IsCorrect == true);

                return new TopicPerformanceDto
                {
                    TopicName = group.Key,
                    TotalQuestions = total,
                    CorrectAnswers = correct,
                    Accuracy = total == 0 ? 0 : Math.Round((correct / (decimal)total) * 100m, 2)
                };
            })
            .OrderByDescending(x => x.Accuracy)
            .ToList();
    }

    private static IReadOnlyCollection<Guid> DeserializeOptionIds(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch
        {
            return Array.Empty<Guid>();
        }
    }
}
