namespace QuizSystem.Core.Entities;

public class Quiz : BaseEntity
{
    public Guid InstructorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int AttemptLimit { get; set; } = 1;
    public decimal PassPercentage { get; set; } = 40;
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public bool NegativeMarkingEnabled { get; set; }
    public int AutoSaveIntervalSeconds { get; set; } = 15;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizAssignment> Assignments { get; set; } = new List<QuizAssignment>();
    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
}
