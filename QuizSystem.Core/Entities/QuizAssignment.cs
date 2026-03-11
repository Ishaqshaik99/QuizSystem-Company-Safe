namespace QuizSystem.Core.Entities;

public class QuizAssignment : BaseEntity
{
    public Guid QuizId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? GroupClassId { get; set; }
    public Guid AssignedByInstructorId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DueAtUtc { get; set; }

    public Quiz? Quiz { get; set; }
    public GroupClass? GroupClass { get; set; }
}
