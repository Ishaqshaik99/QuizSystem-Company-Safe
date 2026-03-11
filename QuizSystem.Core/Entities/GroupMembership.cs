namespace QuizSystem.Core.Entities;

public class GroupMembership : BaseEntity
{
    public Guid GroupClassId { get; set; }
    public Guid StudentId { get; set; }

    public GroupClass? GroupClass { get; set; }
}
