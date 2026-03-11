using QuizSystem.Core.Common;
using QuizSystem.Core.DTOs;

namespace QuizSystem.Core.Interfaces;

public interface IQuestionService
{
    Task<QuestionDto> CreateAsync(Guid instructorId, QuestionCreateUpdateRequest request, CancellationToken cancellationToken = default);
    Task<QuestionDto> UpdateAsync(Guid instructorId, Guid questionId, QuestionCreateUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid instructorId, Guid questionId, CancellationToken cancellationToken = default);
    Task<QuestionDto?> GetByIdAsync(Guid questionId, CancellationToken cancellationToken = default);
    Task<PagedResult<QuestionDto>> QueryAsync(Guid instructorId, QuestionFilterRequest request, CancellationToken cancellationToken = default);
}
