using QuizSystem.Core.DTOs;

namespace QuizSystem.Core.Interfaces;

public interface IReportService
{
    Task<StudentDashboardDto> GetStudentDashboardAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<InstructorQuizAnalyticsDto> GetQuizAnalyticsAsync(Guid requesterId, Guid quizId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<AdminOverviewDto> GetAdminOverviewAsync(CancellationToken cancellationToken = default);
}
