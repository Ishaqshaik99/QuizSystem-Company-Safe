using System.Net;
using Microsoft.EntityFrameworkCore;
using QuizSystem.Core.Common;
using QuizSystem.Core.DTOs;
using QuizSystem.Core.Entities;
using QuizSystem.Core.Enums;
using QuizSystem.Core.Interfaces;
using QuizSystem.Infrastructure.Data;

namespace QuizSystem.Infrastructure.Services;

public class QuestionService : IQuestionService
{
    private readonly QuizSystemDbContext _dbContext;

    public QuestionService(QuizSystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QuestionDto> CreateAsync(Guid instructorId, QuestionCreateUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ValidateQuestionRequest(request);

        var topicId = await ResolveTopicIdAsync(request.TopicId, request.TopicName, cancellationToken);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            InstructorId = instructorId,
            Stem = request.Stem.Trim(),
            Type = request.Type,
            TopicId = topicId,
            Difficulty = request.Difficulty,
            AllowPartialScoring = request.AllowPartialScoring,
            ShortAnswerAutoCheckEnabled = request.ShortAnswerAutoCheckEnabled,
            ShortAnswerCaseSensitive = request.ShortAnswerCaseSensitive,
            ShortAnswerExpectedAnswer = request.ShortAnswerExpectedAnswer?.Trim()
        };

        if (request.Type != QuestionType.ShortAnswer)
        {
            question.Options = request.Options.Select(x => new QuestionOption
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Text = x.Text.Trim(),
                IsCorrect = x.IsCorrect,
                OrderIndex = x.OrderIndex
            }).ToList();
        }

        await _dbContext.Questions.AddAsync(question, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapQuestionDtoAsync(question.Id, cancellationToken)
            ?? throw new AppException("Failed to create question.", HttpStatusCode.InternalServerError);
    }

    public async Task<QuestionDto> UpdateAsync(Guid instructorId, Guid questionId, QuestionCreateUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ValidateQuestionRequest(request);

        var question = await _dbContext.Questions.Include(x => x.Options)
            .SingleOrDefaultAsync(x => x.Id == questionId && x.InstructorId == instructorId, cancellationToken)
            ?? throw new AppException("Question not found.", HttpStatusCode.NotFound);

        question.Stem = request.Stem.Trim();
        question.Type = request.Type;
        question.Difficulty = request.Difficulty;
        question.AllowPartialScoring = request.AllowPartialScoring;
        question.ShortAnswerAutoCheckEnabled = request.ShortAnswerAutoCheckEnabled;
        question.ShortAnswerCaseSensitive = request.ShortAnswerCaseSensitive;
        question.ShortAnswerExpectedAnswer = request.ShortAnswerExpectedAnswer?.Trim();
        question.TopicId = await ResolveTopicIdAsync(request.TopicId, request.TopicName, cancellationToken);
        question.UpdatedAtUtc = DateTime.UtcNow;

        if (question.Type == QuestionType.ShortAnswer)
        {
            _dbContext.QuestionOptions.RemoveRange(question.Options);
        }
        else
        {
            _dbContext.QuestionOptions.RemoveRange(question.Options);
            question.Options = request.Options.Select(x => new QuestionOption
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Text = x.Text.Trim(),
                IsCorrect = x.IsCorrect,
                OrderIndex = x.OrderIndex
            }).ToList();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapQuestionDtoAsync(question.Id, cancellationToken)
            ?? throw new AppException("Question not found.", HttpStatusCode.NotFound);
    }

    public async Task DeleteAsync(Guid instructorId, Guid questionId, CancellationToken cancellationToken = default)
    {
        var question = await _dbContext.Questions
            .SingleOrDefaultAsync(x => x.Id == questionId && x.InstructorId == instructorId, cancellationToken)
            ?? throw new AppException("Question not found.", HttpStatusCode.NotFound);

        _dbContext.Questions.Remove(question);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuestionDto?> GetByIdAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return await MapQuestionDtoAsync(questionId, cancellationToken);
    }

    public async Task<PagedResult<QuestionDto>> QueryAsync(Guid instructorId, QuestionFilterRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Questions.AsNoTracking()
            .Include(x => x.Topic)
            .Include(x => x.Options)
            .Where(x => x.InstructorId == instructorId);

        if (request.TopicId.HasValue)
        {
            query = query.Where(x => x.TopicId == request.TopicId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TopicName))
        {
            query = query.Where(x => x.Topic != null && x.Topic.Name.Contains(request.TopicName));
        }

        if (request.Difficulty.HasValue)
        {
            query = query.Where(x => x.Difficulty == request.Difficulty.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.Type == request.Type.Value);
        }

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(100, request.PageSize);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QuestionDto
            {
                Id = x.Id,
                Stem = x.Stem,
                Type = x.Type,
                TopicId = x.TopicId,
                TopicName = x.Topic != null ? x.Topic.Name : null,
                Difficulty = x.Difficulty,
                AllowPartialScoring = x.AllowPartialScoring,
                ShortAnswerAutoCheckEnabled = x.ShortAnswerAutoCheckEnabled,
                ShortAnswerCaseSensitive = x.ShortAnswerCaseSensitive,
                ShortAnswerExpectedAnswer = x.ShortAnswerExpectedAnswer,
                Options = x.Options.OrderBy(o => o.OrderIndex).Select(o => new QuestionOptionDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    OrderIndex = o.OrderIndex
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<QuestionDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private async Task<Guid?> ResolveTopicIdAsync(Guid? topicId, string? topicName, CancellationToken cancellationToken)
    {
        if (topicId.HasValue)
        {
            var exists = await _dbContext.Topics.AnyAsync(x => x.Id == topicId.Value, cancellationToken);
            if (!exists)
            {
                throw new AppException("Topic not found.", HttpStatusCode.NotFound);
            }

            return topicId.Value;
        }

        if (string.IsNullOrWhiteSpace(topicName))
        {
            return null;
        }

        var normalized = topicName.Trim();
        var topic = await _dbContext.Topics.SingleOrDefaultAsync(x => x.Name == normalized, cancellationToken);
        if (topic is not null)
        {
            return topic.Id;
        }

        var created = new Topic
        {
            Id = Guid.NewGuid(),
            Name = normalized
        };
        await _dbContext.Topics.AddAsync(created, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return created.Id;
    }

    private static void ValidateQuestionRequest(QuestionCreateUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Stem))
        {
            throw new AppException("Question stem is required.");
        }

        if (request.Type != QuestionType.ShortAnswer)
        {
            if (request.Options.Count < 2)
            {
                throw new AppException("At least 2 options are required for objective questions.");
            }

            var correctCount = request.Options.Count(x => x.IsCorrect);
            if (request.Type is QuestionType.McqSingle or QuestionType.TrueFalse)
            {
                if (correctCount != 1)
                {
                    throw new AppException("Single-choice and true/false questions must have exactly one correct option.");
                }
            }
            else if (correctCount < 1)
            {
                throw new AppException("Multiple-choice question must have at least one correct option.");
            }
        }
    }

    private async Task<QuestionDto?> MapQuestionDtoAsync(Guid questionId, CancellationToken cancellationToken)
    {
        return await _dbContext.Questions.AsNoTracking()
            .Include(x => x.Topic)
            .Include(x => x.Options)
            .Where(x => x.Id == questionId)
            .Select(x => new QuestionDto
            {
                Id = x.Id,
                Stem = x.Stem,
                Type = x.Type,
                TopicId = x.TopicId,
                TopicName = x.Topic != null ? x.Topic.Name : null,
                Difficulty = x.Difficulty,
                AllowPartialScoring = x.AllowPartialScoring,
                ShortAnswerAutoCheckEnabled = x.ShortAnswerAutoCheckEnabled,
                ShortAnswerCaseSensitive = x.ShortAnswerCaseSensitive,
                ShortAnswerExpectedAnswer = x.ShortAnswerExpectedAnswer,
                Options = x.Options
                    .OrderBy(o => o.OrderIndex)
                    .Select(o => new QuestionOptionDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect,
                        OrderIndex = o.OrderIndex
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
