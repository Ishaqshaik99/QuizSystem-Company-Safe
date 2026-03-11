using System.Net;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Core.Common;
using QuizSystem.Core.DTOs;
using QuizSystem.Core.Entities;
using QuizSystem.Core.Interfaces;
using QuizSystem.Infrastructure.Data;

namespace QuizSystem.Infrastructure.Services;

public class QuizService : IQuizService
{
    private readonly QuizSystemDbContext _dbContext;

    public QuizService(QuizSystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<QuizDto>> GetAllQuizzesAsync(CancellationToken cancellationToken = default)
    {
        var quizIds = await _dbContext.Quizzes.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var result = new List<QuizDto>(quizIds.Count);
        foreach (var quizId in quizIds)
        {
            var quiz = await MapQuizAsync(quizId, cancellationToken);
            if (quiz is not null)
            {
                result.Add(quiz);
            }
        }

        return result;
    }

    public async Task<QuizDto> CreateAsync(Guid instructorId, QuizCreateUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ValidateQuizRequest(request);

        var questionIds = request.Questions.Select(x => x.QuestionId).Distinct().ToList();
        var availableQuestionIds = await _dbContext.Questions
            .Where(x => x.InstructorId == instructorId && questionIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (availableQuestionIds.Count != questionIds.Count)
        {
            throw new AppException("One or more questions are not accessible.", HttpStatusCode.BadRequest);
        }

        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            InstructorId = instructorId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            DurationMinutes = request.DurationMinutes,
            AttemptLimit = request.AttemptLimit,
            PassPercentage = request.PassPercentage,
            ShuffleQuestions = request.ShuffleQuestions,
            ShuffleOptions = request.ShuffleOptions,
            NegativeMarkingEnabled = request.NegativeMarkingEnabled,
            AutoSaveIntervalSeconds = request.AutoSaveIntervalSeconds,
            QuizQuestions = request.Questions.Select(x => new QuizQuestion
            {
                Id = Guid.NewGuid(),
                QuestionId = x.QuestionId,
                OrderIndex = x.OrderIndex,
                Marks = x.Marks,
                NegativeMarks = x.NegativeMarks
            }).ToList()
        };

        await _dbContext.Quizzes.AddAsync(quiz, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapQuizAsync(quiz.Id, cancellationToken)
            ?? throw new AppException("Failed to create quiz.", HttpStatusCode.InternalServerError);
    }

    public async Task<QuizDto> UpdateAsync(Guid instructorId, Guid quizId, QuizCreateUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ValidateQuizRequest(request);

        var quiz = await _dbContext.Quizzes
            .Include(x => x.QuizQuestions)
            .SingleOrDefaultAsync(x => x.Id == quizId && x.InstructorId == instructorId, cancellationToken)
            ?? throw new AppException("Quiz not found.", HttpStatusCode.NotFound);

        quiz.Title = request.Title.Trim();
        quiz.Description = request.Description?.Trim();
        quiz.DurationMinutes = request.DurationMinutes;
        quiz.AttemptLimit = request.AttemptLimit;
        quiz.PassPercentage = request.PassPercentage;
        quiz.ShuffleQuestions = request.ShuffleQuestions;
        quiz.ShuffleOptions = request.ShuffleOptions;
        quiz.NegativeMarkingEnabled = request.NegativeMarkingEnabled;
        quiz.AutoSaveIntervalSeconds = request.AutoSaveIntervalSeconds;
        quiz.UpdatedAtUtc = DateTime.UtcNow;

        var questionIds = request.Questions.Select(x => x.QuestionId).Distinct().ToList();
        var availableQuestionIds = await _dbContext.Questions
            .Where(x => x.InstructorId == instructorId && questionIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (availableQuestionIds.Count != questionIds.Count)
        {
            throw new AppException("One or more questions are not accessible.", HttpStatusCode.BadRequest);
        }

        _dbContext.QuizQuestions.RemoveRange(quiz.QuizQuestions);
        quiz.QuizQuestions = request.Questions.Select(x => new QuizQuestion
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            QuestionId = x.QuestionId,
            OrderIndex = x.OrderIndex,
            Marks = x.Marks,
            NegativeMarks = x.NegativeMarks
        }).ToList();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapQuizAsync(quiz.Id, cancellationToken)
            ?? throw new AppException("Quiz not found.", HttpStatusCode.NotFound);
    }

    public async Task DeleteAsync(Guid instructorId, Guid quizId, CancellationToken cancellationToken = default)
    {
        var quiz = await _dbContext.Quizzes
            .SingleOrDefaultAsync(x => x.Id == quizId && x.InstructorId == instructorId, cancellationToken)
            ?? throw new AppException("Quiz not found.", HttpStatusCode.NotFound);

        _dbContext.Quizzes.Remove(quiz);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuizDto?> GetByIdAsync(Guid requesterId, Guid quizId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var quiz = await MapQuizAsync(quizId, cancellationToken);
        if (quiz is null)
        {
            return null;
        }

        if (!isAdmin && quiz.InstructorId != requesterId)
        {
            throw new AppException("Not authorized to view this quiz.", HttpStatusCode.Forbidden);
        }

        return quiz;
    }

    public async Task<IReadOnlyCollection<QuizDto>> GetInstructorQuizzesAsync(Guid instructorId, CancellationToken cancellationToken = default)
    {
        var quizzes = await _dbContext.Quizzes.AsNoTracking()
            .Where(x => x.InstructorId == instructorId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var result = new List<QuizDto>(quizzes.Count);
        foreach (var quizId in quizzes)
        {
            var quiz = await MapQuizAsync(quizId, cancellationToken);
            if (quiz is not null)
            {
                result.Add(quiz);
            }
        }

        return result;
    }

    public async Task<IReadOnlyCollection<QuizDto>> GetAssignedQuizzesForStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var groupIds = await _dbContext.GroupMemberships.AsNoTracking()
            .Where(x => x.StudentId == studentId)
            .Select(x => x.GroupClassId)
            .ToListAsync(cancellationToken);

        var quizIds = await _dbContext.QuizAssignments.AsNoTracking()
            .Where(x => x.StudentId == studentId || (x.GroupClassId.HasValue && groupIds.Contains(x.GroupClassId.Value)))
            .Select(x => x.QuizId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var result = new List<QuizDto>(quizIds.Count);
        foreach (var quizId in quizIds)
        {
            var quiz = await MapQuizAsync(quizId, cancellationToken);
            if (quiz is not null && quiz.IsPublished)
            {
                result.Add(quiz);
            }
        }

        return result;
    }

    public async Task PublishAsync(Guid instructorId, Guid quizId, CancellationToken cancellationToken = default)
    {
        var quiz = await _dbContext.Quizzes
            .SingleOrDefaultAsync(x => x.Id == quizId && x.InstructorId == instructorId, cancellationToken)
            ?? throw new AppException("Quiz not found.", HttpStatusCode.NotFound);

        quiz.IsPublished = true;
        quiz.PublishedAtUtc = DateTime.UtcNow;
        quiz.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignAsync(Guid instructorId, AssignQuizRequest request, CancellationToken cancellationToken = default)
    {
        var quiz = await _dbContext.Quizzes
            .SingleOrDefaultAsync(x => x.Id == request.QuizId && x.InstructorId == instructorId, cancellationToken)
            ?? throw new AppException("Quiz not found.", HttpStatusCode.NotFound);

        if (!quiz.IsPublished)
        {
            throw new AppException("Quiz must be published before assignment.", HttpStatusCode.BadRequest);
        }

        var assignments = new List<QuizAssignment>();

        foreach (var studentId in request.StudentIds.Distinct())
        {
            assignments.Add(new QuizAssignment
            {
                Id = Guid.NewGuid(),
                QuizId = request.QuizId,
                StudentId = studentId,
                AssignedByInstructorId = instructorId,
                DueAtUtc = request.DueAtUtc
            });
        }

        foreach (var groupClassId in request.GroupClassIds.Distinct())
        {
            assignments.Add(new QuizAssignment
            {
                Id = Guid.NewGuid(),
                QuizId = request.QuizId,
                GroupClassId = groupClassId,
                AssignedByInstructorId = instructorId,
                DueAtUtc = request.DueAtUtc
            });
        }

        if (assignments.Count == 0)
        {
            throw new AppException("No students or groups provided.");
        }

        var existing = await _dbContext.QuizAssignments.AsNoTracking()
            .Where(x => x.QuizId == request.QuizId)
            .Select(x => new { x.StudentId, x.GroupClassId })
            .ToListAsync(cancellationToken);

        var uniqueAssignments = assignments
            .Where(a => existing.All(e => e.StudentId != a.StudentId || e.GroupClassId != a.GroupClassId))
            .ToList();

        if (uniqueAssignments.Count > 0)
        {
            await _dbContext.QuizAssignments.AddRangeAsync(uniqueAssignments, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<GroupClassDto> CreateGroupAsync(Guid instructorId, GroupClassCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppException("Group name is required.");
        }

        var group = new GroupClass
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            InstructorId = instructorId,
            Members = request.StudentIds.Distinct().Select(x => new GroupMembership
            {
                Id = Guid.NewGuid(),
                StudentId = x
            }).ToList()
        };

        await _dbContext.GroupClasses.AddAsync(group, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GroupClassDto
        {
            Id = group.Id,
            Name = group.Name,
            InstructorId = group.InstructorId,
            StudentIds = group.Members.Select(x => x.StudentId).ToList()
        };
    }

    public async Task<IReadOnlyCollection<GroupClassDto>> GetGroupsAsync(Guid instructorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GroupClasses.AsNoTracking()
            .Include(x => x.Members)
            .Where(x => x.InstructorId == instructorId)
            .OrderBy(x => x.Name)
            .Select(x => new GroupClassDto
            {
                Id = x.Id,
                Name = x.Name,
                InstructorId = x.InstructorId,
                StudentIds = x.Members.Select(m => m.StudentId).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<QuizDto?> MapQuizAsync(Guid quizId, CancellationToken cancellationToken)
    {
        return await _dbContext.Quizzes.AsNoTracking()
            .Include(x => x.QuizQuestions)
                .ThenInclude(x => x.Question)
                    .ThenInclude(x => x!.Topic)
            .Where(x => x.Id == quizId)
            .Select(x => new QuizDto
            {
                Id = x.Id,
                InstructorId = x.InstructorId,
                Title = x.Title,
                Description = x.Description,
                DurationMinutes = x.DurationMinutes,
                AttemptLimit = x.AttemptLimit,
                PassPercentage = x.PassPercentage,
                ShuffleQuestions = x.ShuffleQuestions,
                ShuffleOptions = x.ShuffleOptions,
                NegativeMarkingEnabled = x.NegativeMarkingEnabled,
                AutoSaveIntervalSeconds = x.AutoSaveIntervalSeconds,
                IsPublished = x.IsPublished,
                PublishedAtUtc = x.PublishedAtUtc,
                Questions = x.QuizQuestions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new QuizQuestionViewDto
                    {
                        QuestionId = q.QuestionId,
                        OrderIndex = q.OrderIndex,
                        Marks = q.Marks,
                        NegativeMarks = q.NegativeMarks,
                        Stem = q.Question != null ? q.Question.Stem : string.Empty,
                        QuestionType = q.Question != null ? q.Question.Type.ToString() : string.Empty,
                        TopicName = q.Question != null && q.Question.Topic != null ? q.Question.Topic.Name : null
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static void ValidateQuizRequest(QuizCreateUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new AppException("Quiz title is required.");
        }

        if (request.DurationMinutes <= 0)
        {
            throw new AppException("Duration must be greater than zero.");
        }

        if (request.AttemptLimit <= 0)
        {
            throw new AppException("Attempt limit must be greater than zero.");
        }

        if (request.AutoSaveIntervalSeconds < 5 || request.AutoSaveIntervalSeconds > 120)
        {
            throw new AppException("Autosave interval must be between 5 and 120 seconds.");
        }

        if (request.Questions.Count == 0)
        {
            throw new AppException("At least one question is required.");
        }
    }
}
