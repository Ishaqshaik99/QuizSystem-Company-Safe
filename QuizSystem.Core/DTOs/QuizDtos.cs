namespace QuizSystem.Core.DTOs;

public class QuizQuestionInputDto
{
    public Guid QuestionId { get; set; }
    public int OrderIndex { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
}

public class QuizCreateUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int AttemptLimit { get; set; } = 1;
    public decimal PassPercentage { get; set; } = 40;
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public bool NegativeMarkingEnabled { get; set; }
    public int AutoSaveIntervalSeconds { get; set; } = 15;
    public IReadOnlyCollection<QuizQuestionInputDto> Questions { get; set; } = Array.Empty<QuizQuestionInputDto>();
}

public class QuizDto
{
    public Guid Id { get; set; }
    public Guid InstructorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int AttemptLimit { get; set; }
    public decimal PassPercentage { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public bool NegativeMarkingEnabled { get; set; }
    public int AutoSaveIntervalSeconds { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public IReadOnlyCollection<QuizQuestionViewDto> Questions { get; set; } = Array.Empty<QuizQuestionViewDto>();
}

public class QuizQuestionViewDto
{
    public Guid QuestionId { get; set; }
    public int OrderIndex { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public string Stem { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? TopicName { get; set; }
}

public class AssignQuizRequest
{
    public Guid QuizId { get; set; }
    public IReadOnlyCollection<Guid> StudentIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyCollection<Guid> GroupClassIds { get; set; } = Array.Empty<Guid>();
    public DateTime? DueAtUtc { get; set; }
}

public class GroupClassCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyCollection<Guid> StudentIds { get; set; } = Array.Empty<Guid>();
}

public class GroupClassDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid InstructorId { get; set; }
    public IReadOnlyCollection<Guid> StudentIds { get; set; } = Array.Empty<Guid>();
}
