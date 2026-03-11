namespace QuizSystem.Core.Entities;

public class QuestionOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }

    public Question? Question { get; set; }
}
