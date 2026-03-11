using QuizSystem.Core.DTOs;

namespace QuizSystem.Core.Interfaces;

public interface IAttemptService
{
    Task<IReadOnlyCollection<AttemptResultDto>> GetAllAttemptsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttemptResultDto>> GetAttemptsByQuizAsync(Guid requesterId, Guid quizId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<AttemptSessionDto> StartAttemptAsync(Guid studentId, StartAttemptRequest request, CancellationToken cancellationToken = default);
    Task<AttemptSessionDto?> GetActiveSessionAsync(Guid studentId, Guid attemptId, CancellationToken cancellationToken = default);
    Task SaveAnswerAsync(Guid studentId, Guid attemptId, SaveAnswerRequest request, CancellationToken cancellationToken = default);
    Task<AttemptResultDto> SubmitAsync(Guid studentId, Guid attemptId, SubmitAttemptRequest request, CancellationToken cancellationToken = default);
    Task<int> AutoSubmitExpiredAsync(CancellationToken cancellationToken = default);
    Task<AttemptResultDto?> GetAttemptDetailForStudentAsync(Guid studentId, Guid attemptId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AttemptResultDto>> GetStudentAttemptsAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<AttemptResultDto> GradeShortAnswerAsync(Guid instructorId, Guid attemptId, ManualGradeRequest request, CancellationToken cancellationToken = default);
}
