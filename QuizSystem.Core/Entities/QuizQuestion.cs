namespace QuizSystem.Core.Entities;

public class QuizQuestion : BaseEntity
{
    public Guid QuizId { get; set; }
    public Guid QuestionId { get; set; }
    public int OrderIndex { get; set; }
    public decimal Marks { get; set; }
    public decimal NegativeMarks { get; set; }

    public Quiz? Quiz { get; set; }
    public Question? Question { get; set; }
}
