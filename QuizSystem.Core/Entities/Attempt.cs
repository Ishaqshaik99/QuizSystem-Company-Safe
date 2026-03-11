using QuizSystem.Core.Enums;

namespace QuizSystem.Core.Entities;

public class Attempt : BaseEntity
{
    public Guid QuizId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public bool IsAutoSubmitted { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage { get; set; }

    public Quiz? Quiz { get; set; }
    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}
