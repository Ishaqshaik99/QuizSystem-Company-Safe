using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Core.Common;
using QuizSystem.Core.DTOs;
using QuizSystem.Core.Entities;
using QuizSystem.Core.Enums;
using QuizSystem.Core.Interfaces;
using QuizSystem.Core.Rules;
using QuizSystem.Infrastructure.Data;

namespace QuizSystem.Infrastructure.Services;

public class AttemptService : IAttemptService
{
    private readonly QuizSystemDbContext _dbContext;

    public AttemptService(QuizSystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<AttemptResultDto>> GetAllAttemptsAsync(CancellationToken cancellationToken = default)
    {
        var attemptIds = await _dbContext.Attempts.AsNoTracking()
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var results = new List<AttemptResultDto>(attemptIds.Count);
        foreach (var attemptId in attemptIds)
        {
            var dto = await BuildAttemptResultAsync(attemptId, cancellationToken);
            if (dto is not null)
            {
                results.Add(dto);
            }
        }

        return results;
    }

    public async Task<IReadOnlyCollection<AttemptResultDto>> GetAttemptsByQuizAsync(Guid requesterId, Guid quizId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (!isAdmin)
        {
            var ownsQuiz = await _dbContext.Quizzes.AsNoTracking()
                .AnyAsync(x => x.Id == quizId && x.InstructorId == requesterId, cancellationToken);

            if (!ownsQuiz)
            {
                throw new AppException("Not allowed to view attempts for this quiz.", HttpStatusCode.Forbidden);
            }
        }

        var attemptIds = await _dbContext.Attempts.AsNoTracking()
            .Where(x => x.QuizId == quizId)
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var results = new List<AttemptResultDto>(attemptIds.Count);
        foreach (var attemptId in attemptIds)
        {
            var dto = await BuildAttemptResultAsync(attemptId, cancellationToken);
            if (dto is not null)
            {
                results.Add(dto);
            }
        }

        return results;
    }

    public async Task<AttemptSessionDto> StartAttemptAsync(Guid studentId, StartAttemptRequest request, CancellationToken cancellationToken = default)
    {
        var quiz = await _dbContext.Quizzes
            .Include(x => x.QuizQuestions)
                .ThenInclude(x => x.Question)
                    .ThenInclude(x => x!.Options)
            .Include(x => x.QuizQuestions)
                .ThenInclude(x => x.Question)
                    .ThenInclude(x => x!.Topic)
            .SingleOrDefaultAsync(x => x.Id == request.QuizId, cancellationToken)
            ?? throw new AppException("Quiz not found.", HttpStatusCode.NotFound);

        if (!quiz.IsPublished)
        {
            throw new AppException("Quiz is not published.", HttpStatusCode.BadRequest);
        }

        var assigned = await IsStudentAssignedToQuizAsync(studentId, quiz.Id, cancellationToken);
        if (!assigned)
        {
            throw new AppException("Quiz is not assigned to this student.", HttpStatusCode.Forbidden);
        }

        var inProgress = await _dbContext.Attempts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.QuizId == quiz.Id && x.StudentId == studentId && x.Status == AttemptStatus.InProgress, cancellationToken);

        if (inProgress is not null)
        {
            return await BuildAttemptSessionAsync(inProgress, quiz, cancellationToken);
        }

        var completedAttempts = await _dbContext.Attempts
            .CountAsync(x => x.QuizId == quiz.Id && x.StudentId == studentId && x.Status != AttemptStatus.InProgress, cancellationToken);

        if (completedAttempts >= quiz.AttemptLimit)
        {
            throw new AppException("Attempt limit reached.", HttpStatusCode.BadRequest);
        }

        var attempt = new Attempt
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            StudentId = studentId,
            StartedAtUtc = DateTime.UtcNow,
            EndsAtUtc = DateTime.UtcNow.AddMinutes(quiz.DurationMinutes),
            Status = AttemptStatus.InProgress
        };

        await _dbContext.Attempts.AddAsync(attempt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildAttemptSessionAsync(attempt, quiz, cancellationToken);
    }

    public async Task<AttemptSessionDto?> GetActiveSessionAsync(Guid studentId, Guid attemptId, CancellationToken cancellationToken = default)
    {
        var attempt = await _dbContext.Attempts
            .Include(x => x.Quiz)
                .ThenInclude(x => x!.QuizQuestions)
                    .ThenInclude(x => x.Question)
                        .ThenInclude(x => x!.Options)
            .Include(x => x.Quiz)
                .ThenInclude(x => x!.QuizQuestions)
                    .ThenInclude(x => x.Question)
                        .ThenInclude(x => x!.Topic)
            .SingleOrDefaultAsync(x => x.Id == attemptId && x.StudentId == studentId, cancellationToken);

        if (attempt?.Quiz is null)
        {
            return null;
        }

        if (attempt.Status != AttemptStatus.InProgress)
        {
            return null;
        }

        if (DateTime.UtcNow > attempt.EndsAtUtc)
        {
            await FinalizeAttemptAsync(attempt, autoSubmitted: true, cancellationToken);
            return null;
        }

        return await BuildAttemptSessionAsync(attempt, attempt.Quiz, cancellationToken);
    }

    public async Task SaveAnswerAsync(Guid studentId, Guid attemptId, SaveAnswerRequest request, CancellationToken cancellationToken = default)
    {
        var attempt = await _dbContext.Attempts
            .Include(x => x.Quiz)
            .SingleOrDefaultAsync(x => x.Id == attemptId && x.StudentId == studentId, cancellationToken)
            ?? throw new AppException("Attempt not found.", HttpStatusCode.NotFound);

        AttemptTimingGuard.EnsureAttemptCanBeModified(attempt, DateTime.UtcNow);

        var quizQuestion = await _dbContext.QuizQuestions
            .Include(x => x.Question)
                .ThenInclude(q => q!.Options)
            .SingleOrDefaultAsync(x => x.QuizId == attempt.QuizId && x.QuestionId == request.QuestionId, cancellationToken)
            ?? throw new AppException("Question not part of this quiz.", HttpStatusCode.BadRequest);

        if (quizQuestion.Question is null)
        {
            throw new AppException("Question not found.", HttpStatusCode.NotFound);
        }

        ValidateAnswerPayload(quizQuestion.Question.Type, request);

        var answer = await _dbContext.AttemptAnswers
            .SingleOrDefaultAsync(x => x.AttemptId == attemptId && x.QuestionId == request.QuestionId, cancellationToken);

        if (answer is null)
        {
            answer = new AttemptAnswer
            {
                Id = Guid.NewGuid(),
                AttemptId = attemptId,
                QuestionId = request.QuestionId
            };
            await _dbContext.AttemptAnswers.AddAsync(answer, cancellationToken);
        }

        answer.SelectedOptionIdsJson = request.SelectedOptionIds is null
            ? null
            : JsonSerializer.Serialize(request.SelectedOptionIds.Distinct());
        answer.ShortAnswerText = request.ShortAnswerText?.Trim();
        answer.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AttemptResultDto> SubmitAsync(Guid studentId, Guid attemptId, SubmitAttemptRequest request, CancellationToken cancellationToken = default)
    {
        var attempt = await _dbContext.Attempts
            .Include(x => x.Quiz)
            .SingleOrDefaultAsync(x => x.Id == attemptId && x.StudentId == studentId, cancellationToken)
            ?? throw new AppException("Attempt not found.", HttpStatusCode.NotFound);

        if (attempt.Status != AttemptStatus.InProgress)
        {
            return await BuildAttemptResultAsync(attemptId, cancellationToken)
                ?? throw new AppException("Attempt result not found.", HttpStatusCode.NotFound);
        }

        var now = DateTime.UtcNow;
        var isLate = now > attempt.EndsAtUtc;

        if (isLate && !request.ForceSubmit)
        {
            throw new AppException("Cannot submit after time ends. Use auto-submit flow.", HttpStatusCode.BadRequest);
        }

        await FinalizeAttemptAsync(attempt, isLate, cancellationToken);

        return await BuildAttemptResultAsync(attempt.Id, cancellationToken)
            ?? throw new AppException("Attempt result not found.", HttpStatusCode.NotFound);
    }

    public async Task<int> AutoSubmitExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredAttempts = await _dbContext.Attempts
            .Where(x => x.Status == AttemptStatus.InProgress && x.EndsAtUtc <= now)
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var attempt in expiredAttempts)
        {
            await FinalizeAttemptAsync(attempt, autoSubmitted: true, cancellationToken);
            count++;
        }

        return count;
    }

    public async Task<AttemptResultDto?> GetAttemptDetailForStudentAsync(Guid studentId, Guid attemptId, CancellationToken cancellationToken = default)
    {
        var attemptExists = await _dbContext.Attempts
            .AnyAsync(x => x.Id == attemptId && x.StudentId == studentId, cancellationToken);

        if (!attemptExists)
        {
            return null;
        }

        return await BuildAttemptResultAsync(attemptId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AttemptResultDto>> GetStudentAttemptsAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var attemptIds = await _dbContext.Attempts.AsNoTracking()
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var results = new List<AttemptResultDto>(attemptIds.Count);
        foreach (var attemptId in attemptIds)
        {
            var dto = await BuildAttemptResultAsync(attemptId, cancellationToken);
            if (dto is not null)
            {
                results.Add(dto);
            }
        }

        return results;
    }

    public async Task<AttemptResultDto> GradeShortAnswerAsync(Guid instructorId, Guid attemptId, ManualGradeRequest request, CancellationToken cancellationToken = default)
    {
        var attempt = await _dbContext.Attempts
            .Include(x => x.Quiz)
            .SingleOrDefaultAsync(x => x.Id == attemptId, cancellationToken)
            ?? throw new AppException("Attempt not found.", HttpStatusCode.NotFound);

        if (attempt.Quiz is null || attempt.Quiz.InstructorId != instructorId)
        {
            throw new AppException("Not allowed to grade this attempt.", HttpStatusCode.Forbidden);
        }

        var quizQuestion = await _dbContext.QuizQuestions
            .Include(x => x.Question)
            .SingleOrDefaultAsync(x => x.QuizId == attempt.QuizId && x.QuestionId == request.QuestionId, cancellationToken)
            ?? throw new AppException("Question not in quiz.", HttpStatusCode.BadRequest);

        if (quizQuestion.Question?.Type != QuestionType.ShortAnswer)
        {
            throw new AppException("Only short answers can be manually graded.");
        }

        if (request.AwardedScore < 0 || request.AwardedScore > quizQuestion.Marks)
        {
            throw new AppException($"Awarded score must be between 0 and {quizQuestion.Marks}.");
        }

        var answer = await _dbContext.AttemptAnswers
            .SingleOrDefaultAsync(x => x.AttemptId == attemptId && x.QuestionId == request.QuestionId, cancellationToken)
            ?? throw new AppException("Answer not found.", HttpStatusCode.NotFound);

        answer.AwardedScore = request.AwardedScore;
        answer.IsCorrect = request.IsCorrect;
        answer.IsManuallyGraded = true;
        answer.GradedByInstructorId = instructorId;
        answer.GradedAtUtc = DateTime.UtcNow;
        answer.Notes = request.Notes?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateFinalScoreAsync(attempt, cancellationToken);

        return await BuildAttemptResultAsync(attemptId, cancellationToken)
            ?? throw new AppException("Attempt result not found.", HttpStatusCode.NotFound);
    }

    private async Task<bool> IsStudentAssignedToQuizAsync(Guid studentId, Guid quizId, CancellationToken cancellationToken)
    {
        var groupIds = await _dbContext.GroupMemberships.AsNoTracking()
            .Where(x => x.StudentId == studentId)
            .Select(x => x.GroupClassId)
            .ToListAsync(cancellationToken);

        return await _dbContext.QuizAssignments.AsNoTracking()
            .AnyAsync(x => x.QuizId == quizId &&
                (x.StudentId == studentId || (x.GroupClassId.HasValue && groupIds.Contains(x.GroupClassId.Value))),
                cancellationToken);
    }

    private async Task<AttemptSessionDto> BuildAttemptSessionAsync(Attempt attempt, Quiz quiz, CancellationToken cancellationToken)
    {
        if (attempt.Status == AttemptStatus.InProgress && DateTime.UtcNow > attempt.EndsAtUtc)
        {
            await FinalizeAttemptAsync(attempt, autoSubmitted: true, cancellationToken);
            throw new AppException("Attempt time expired and has been auto-submitted.", HttpStatusCode.BadRequest);
        }

        var random = new Random(attempt.Id.GetHashCode());

        var questions = quiz.QuizQuestions.OrderBy(x => x.OrderIndex).ToList();
        if (quiz.ShuffleQuestions)
        {
            questions = questions.OrderBy(_ => random.Next()).ToList();
        }

        var questionDtos = new List<AttemptQuestionDto>(questions.Count);
        foreach (var quizQuestion in questions)
        {
            var question = quizQuestion.Question;
            if (question is null)
            {
                continue;
            }

            var options = question.Options.OrderBy(x => x.OrderIndex).ToList();
            if (quiz.ShuffleOptions)
            {
                options = options.OrderBy(_ => random.Next()).ToList();
            }

            questionDtos.Add(new AttemptQuestionDto
            {
                QuestionId = question.Id,
                Stem = question.Stem,
                Type = question.Type,
                Marks = quizQuestion.Marks,
                NegativeMarks = quizQuestion.NegativeMarks,
                TopicName = question.Topic?.Name,
                Options = options.Select(x => new AttemptOptionDto
                {
                    OptionId = x.Id,
                    Text = x.Text
                }).ToList()
            });
        }

        return new AttemptSessionDto
        {
            AttemptId = attempt.Id,
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            StartedAtUtc = attempt.StartedAtUtc,
            EndsAtUtc = attempt.EndsAtUtc,
            RemainingSeconds = AttemptTimingGuard.RemainingSeconds(attempt, DateTime.UtcNow),
            AutoSaveIntervalSeconds = quiz.AutoSaveIntervalSeconds,
            Questions = questionDtos
        };
    }

    private async Task FinalizeAttemptAsync(Attempt attempt, bool autoSubmitted, CancellationToken cancellationToken)
    {
        var quiz = await _dbContext.Quizzes
            .Include(x => x.QuizQuestions)
                .ThenInclude(x => x.Question)
                    .ThenInclude(q => q!.Options)
            .SingleOrDefaultAsync(x => x.Id == attempt.QuizId, cancellationToken)
            ?? throw new AppException("Quiz not found.", HttpStatusCode.NotFound);

        var answers = await _dbContext.AttemptAnswers
            .Where(x => x.AttemptId == attempt.Id)
            .ToListAsync(cancellationToken);

        decimal score = 0;
        decimal maxScore = 0;
        var hasPendingManual = false;

        foreach (var quizQuestion in quiz.QuizQuestions)
        {
            maxScore += quizQuestion.Marks;
            var question = quizQuestion.Question;
            if (question is null)
            {
                continue;
            }

            var answer = answers.SingleOrDefault(x => x.QuestionId == quizQuestion.QuestionId);
            if (answer is null)
            {
                continue;
            }

            if (question.Type == QuestionType.ShortAnswer)
            {
                if (question.ShortAnswerAutoCheckEnabled)
                {
                    var isCorrect = PartialScoringRule.IsShortAnswerCorrect(
                        question.ShortAnswerExpectedAnswer,
                        answer.ShortAnswerText,
                        question.ShortAnswerCaseSensitive);

                    answer.IsCorrect = isCorrect;
                    answer.AwardedScore = isCorrect ? quizQuestion.Marks : 0;
                    answer.IsManuallyGraded = false;
                    score += answer.AwardedScore.Value;
                }
                else
                {
                    hasPendingManual = true;
                    answer.IsCorrect = null;
                    answer.AwardedScore ??= 0;
                }

                answer.UpdatedAtUtc = DateTime.UtcNow;
                continue;
            }

            var selectedOptionIds = ParseSelectedOptions(answer.SelectedOptionIdsJson);
            var correctOptionIds = question.Options.Where(x => x.IsCorrect).Select(x => x.Id).ToList();

            var perQuestionNegative = quiz.NegativeMarkingEnabled ? quizQuestion.NegativeMarks : 0;
            var calculated = PartialScoringRule.CalculateObjectiveScore(
                question.Type,
                correctOptionIds,
                selectedOptionIds,
                quizQuestion.Marks,
                perQuestionNegative,
                question.AllowPartialScoring);

            answer.AwardedScore = calculated;
            answer.IsCorrect = calculated > 0 && question.Type != QuestionType.McqMultiple
                ? selectedOptionIds.Count == correctOptionIds.Count && selectedOptionIds.All(correctOptionIds.Contains)
                : selectedOptionIds.Count > 0 && Math.Abs(calculated - quizQuestion.Marks) < 0.0001m;
            answer.IsManuallyGraded = false;
            answer.UpdatedAtUtc = DateTime.UtcNow;

            score += calculated;
        }

        attempt.Score = Math.Round(score, 2, MidpointRounding.AwayFromZero);
        attempt.MaxScore = Math.Round(maxScore, 2, MidpointRounding.AwayFromZero);
        attempt.Percentage = maxScore == 0 ? 0 : Math.Round((score / maxScore) * 100m, 2, MidpointRounding.AwayFromZero);
        attempt.SubmittedAtUtc = DateTime.UtcNow;
        attempt.IsAutoSubmitted = autoSubmitted;
        attempt.Status = hasPendingManual
            ? (autoSubmitted ? AttemptStatus.AutoSubmitted : AttemptStatus.Submitted)
            : AttemptStatus.Evaluated;
        attempt.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.Attempts.Update(attempt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecalculateFinalScoreAsync(Attempt attempt, CancellationToken cancellationToken)
    {
        var quiz = await _dbContext.Quizzes
            .Include(x => x.QuizQuestions)
            .SingleOrDefaultAsync(x => x.Id == attempt.QuizId, cancellationToken)
            ?? throw new AppException("Quiz not found.", HttpStatusCode.NotFound);

        var answers = await _dbContext.AttemptAnswers
            .Where(x => x.AttemptId == attempt.Id)
            .ToListAsync(cancellationToken);

        var maxScore = quiz.QuizQuestions.Sum(x => x.Marks);
        var score = answers.Sum(x => x.AwardedScore ?? 0);
        var pendingManual = quiz.QuizQuestions.Any(q =>
        {
            var answer = answers.SingleOrDefault(a => a.QuestionId == q.QuestionId);
            return answer is not null && answer.IsCorrect is null;
        });

        attempt.MaxScore = Math.Round(maxScore, 2, MidpointRounding.AwayFromZero);
        attempt.Score = Math.Round(score, 2, MidpointRounding.AwayFromZero);
        attempt.Percentage = maxScore == 0 ? 0 : Math.Round((score / maxScore) * 100m, 2, MidpointRounding.AwayFromZero);
        attempt.Status = pendingManual ? AttemptStatus.Submitted : AttemptStatus.Evaluated;
        attempt.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.Attempts.Update(attempt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AttemptResultDto?> BuildAttemptResultAsync(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _dbContext.Attempts.AsNoTracking()
            .Include(x => x.Quiz)
            .SingleOrDefaultAsync(x => x.Id == attemptId, cancellationToken);

        if (attempt is null || attempt.Quiz is null)
        {
            return null;
        }

        var answers = await _dbContext.AttemptAnswers.AsNoTracking()
            .Where(x => x.AttemptId == attemptId)
            .ToListAsync(cancellationToken);

        var quizQuestions = await _dbContext.QuizQuestions.AsNoTracking()
            .Include(x => x.Question)
                .ThenInclude(q => q!.Topic)
            .Where(x => x.QuizId == attempt.QuizId)
            .ToListAsync(cancellationToken);

        var answerDtos = new List<AttemptAnswerDto>(quizQuestions.Count);
        foreach (var quizQuestion in quizQuestions)
        {
            var question = quizQuestion.Question;
            if (question is null)
            {
                continue;
            }

            var answer = answers.SingleOrDefault(x => x.QuestionId == question.Id);

            answerDtos.Add(new AttemptAnswerDto
            {
                QuestionId = question.Id,
                Stem = question.Stem,
                QuestionType = question.Type.ToString(),
                TopicName = question.Topic?.Name,
                SelectedOptionIds = ParseSelectedOptions(answer?.SelectedOptionIdsJson),
                ShortAnswerText = answer?.ShortAnswerText,
                IsCorrect = answer?.IsCorrect,
                AwardedScore = answer?.AwardedScore,
                RequiresManualGrading = question.Type == QuestionType.ShortAnswer && !question.ShortAnswerAutoCheckEnabled
            });
        }

        return new AttemptResultDto
        {
            AttemptId = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz.Title,
            Status = attempt.Status,
            StartedAtUtc = attempt.StartedAtUtc,
            SubmittedAtUtc = attempt.SubmittedAtUtc,
            Score = attempt.Score,
            MaxScore = attempt.MaxScore,
            Percentage = attempt.Percentage,
            Passed = attempt.Percentage >= attempt.Quiz.PassPercentage,
            Answers = answerDtos
        };
    }

    private static List<Guid> ParseSelectedOptions(string? selectedOptionIdsJson)
    {
        if (string.IsNullOrWhiteSpace(selectedOptionIdsJson))
        {
            return new List<Guid>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(selectedOptionIdsJson) ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }

    private static void ValidateAnswerPayload(QuestionType type, SaveAnswerRequest request)
    {
        if (type == QuestionType.ShortAnswer)
        {
            return;
        }

        if (request.SelectedOptionIds is null)
        {
            throw new AppException("Selected options are required for objective questions.");
        }

        if ((type is QuestionType.McqSingle or QuestionType.TrueFalse) && request.SelectedOptionIds.Count > 1)
        {
            throw new AppException("Only one option can be selected for this question.");
        }
    }
}
