namespace QuizSystem.Core.Entities;

public class AttemptAnswer : BaseEntity
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string? SelectedOptionIdsJson { get; set; }
    public string? ShortAnswerText { get; set; }
    public bool? IsCorrect { get; set; }
    public decimal? AwardedScore { get; set; }
    public bool IsManuallyGraded { get; set; }
    public Guid? GradedByInstructorId { get; set; }
    public DateTime? GradedAtUtc { get; set; }
    public string? Notes { get; set; }

    public Attempt? Attempt { get; set; }
    public Question? Question { get; set; }
}
