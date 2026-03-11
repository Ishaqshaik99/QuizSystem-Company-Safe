namespace QuizSystem.Core.Entities;

public class GroupClass : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid InstructorId { get; set; }

    public ICollection<GroupMembership> Members { get; set; } = new List<GroupMembership>();
    public ICollection<QuizAssignment> Assignments { get; set; } = new List<QuizAssignment>();
}
