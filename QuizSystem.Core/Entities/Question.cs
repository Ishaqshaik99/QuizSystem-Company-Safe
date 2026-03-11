using QuizSystem.Core.Enums;

namespace QuizSystem.Core.Entities;

public class Question : BaseEntity
{
    public Guid InstructorId { get; set; }
    public string Stem { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public Guid? TopicId { get; set; }
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
    public bool AllowPartialScoring { get; set; }
    public bool ShortAnswerAutoCheckEnabled { get; set; }
    public bool ShortAnswerCaseSensitive { get; set; }
    public string? ShortAnswerExpectedAnswer { get; set; }

    public Topic? Topic { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
}
