using QuizSystem.Core.Enums;

namespace QuizSystem.Core.DTOs;

public class StartAttemptRequest
{
    public Guid QuizId { get; set; }
}

public class SaveAnswerRequest
{
    public Guid QuestionId { get; set; }
    public IReadOnlyCollection<Guid>? SelectedOptionIds { get; set; }
    public string? ShortAnswerText { get; set; }
}

public class SubmitAttemptRequest
{
    public bool ForceSubmit { get; set; }
}

public class ManualGradeRequest
{
    public Guid QuestionId { get; set; }
    public decimal AwardedScore { get; set; }
    public bool IsCorrect { get; set; }
    public string? Notes { get; set; }
}

public class AttemptSessionDto
{
    public Guid AttemptId { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public int RemainingSeconds { get; set; }
    public int AutoSaveIntervalSeconds { get; set; }
    public IReadOnlyCollection<AttemptQuestionDto> Questions { get; set; } = Array.Empty<AttemptQuestionDto>();
}

public class AttemptQuestionDto
{
    public Guid QuestionId { get; set; }
    public string Stem { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }
    public string? TopicName { get; set; }
    public IReadOnlyCollection<AttemptOptionDto> Options { get; set; } = Array.Empty<AttemptOptionDto>();
}

public class AttemptOptionDto
{
    public Guid OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class AttemptAnswerDto
{
    public Guid QuestionId { get; set; }
    public string Stem { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? TopicName { get; set; }
    public IReadOnlyCollection<Guid> SelectedOptionIds { get; set; } = Array.Empty<Guid>();
    public string? ShortAnswerText { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal? AwardedScore { get; set; }
    public bool RequiresManualGrading { get; set; }
}

public class AttemptResultDto
{
    public Guid AttemptId { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public AttemptStatus Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage { get; set; }
    public bool Passed { get; set; }
    public IReadOnlyCollection<AttemptAnswerDto> Answers { get; set; } = Array.Empty<AttemptAnswerDto>();
}
