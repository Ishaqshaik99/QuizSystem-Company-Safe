using QuizSystem.Core.Enums;

namespace QuizSystem.Core.DTOs;

public class QuestionOptionDto
{
    public Guid? Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}

public class QuestionCreateUpdateRequest
{
    public string Stem { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public bool AllowPartialScoring { get; set; }
    public bool ShortAnswerAutoCheckEnabled { get; set; }
    public bool ShortAnswerCaseSensitive { get; set; }
    public string? ShortAnswerExpectedAnswer { get; set; }
    public IReadOnlyCollection<QuestionOptionDto> Options { get; set; } = Array.Empty<QuestionOptionDto>();
}

public class QuestionFilterRequest
{
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public DifficultyLevel? Difficulty { get; set; }
    public QuestionType? Type { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Stem { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public Guid? TopicId { get; set; }
    public string? TopicName { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public bool AllowPartialScoring { get; set; }
    public bool ShortAnswerAutoCheckEnabled { get; set; }
    public bool ShortAnswerCaseSensitive { get; set; }
    public string? ShortAnswerExpectedAnswer { get; set; }
    public IReadOnlyCollection<QuestionOptionDto> Options { get; set; } = Array.Empty<QuestionOptionDto>();
}
