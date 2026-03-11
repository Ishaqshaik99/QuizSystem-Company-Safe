using QuizSystem.Core.DTOs;

namespace QuizSystem.Core.Interfaces;

public interface IQuizService
{
    Task<IReadOnlyCollection<QuizDto>> GetAllQuizzesAsync(CancellationToken cancellationToken = default);
    Task<QuizDto> CreateAsync(Guid instructorId, QuizCreateUpdateRequest request, CancellationToken cancellationToken = default);
    Task<QuizDto> UpdateAsync(Guid instructorId, Guid quizId, QuizCreateUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid instructorId, Guid quizId, CancellationToken cancellationToken = default);
    Task<QuizDto?> GetByIdAsync(Guid requesterId, Guid quizId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<QuizDto>> GetInstructorQuizzesAsync(Guid instructorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<QuizDto>> GetAssignedQuizzesForStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task PublishAsync(Guid instructorId, Guid quizId, CancellationToken cancellationToken = default);
    Task AssignAsync(Guid instructorId, AssignQuizRequest request, CancellationToken cancellationToken = default);
    Task<GroupClassDto> CreateGroupAsync(Guid instructorId, GroupClassCreateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GroupClassDto>> GetGroupsAsync(Guid instructorId, CancellationToken cancellationToken = default);
}
