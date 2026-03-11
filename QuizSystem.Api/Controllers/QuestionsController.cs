using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Api.Extensions;
using QuizSystem.Core.Common;
using QuizSystem.Core.DTOs;
using QuizSystem.Core.Enums;
using QuizSystem.Core.Interfaces;

namespace QuizSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;

    public QuestionsController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<QuestionDto>>> Query(
        [FromQuery] Guid? topicId,
        [FromQuery] string? topicName,
        [FromQuery] DifficultyLevel? difficulty,
        [FromQuery] QuestionType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _questionService.QueryAsync(
            User.GetUserId(),
            new QuestionFilterRequest
            {
                TopicId = topicId,
                TopicName = topicName,
                Difficulty = difficulty,
                Type = type,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{questionId:guid}")]
    public async Task<ActionResult<QuestionDto>> GetById(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await _questionService.GetByIdAsync(questionId, cancellationToken);
        return question is null ? NotFound() : Ok(question);
    }

    [HttpPost]
    public async Task<ActionResult<QuestionDto>> Create([FromBody] QuestionCreateUpdateRequest request, CancellationToken cancellationToken)
    {
        var created = await _questionService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return Ok(created);
    }

    [HttpPut("{questionId:guid}")]
    public async Task<ActionResult<QuestionDto>> Update(Guid questionId, [FromBody] QuestionCreateUpdateRequest request, CancellationToken cancellationToken)
    {
        var updated = await _questionService.UpdateAsync(User.GetUserId(), questionId, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{questionId:guid}")]
    public async Task<IActionResult> Delete(Guid questionId, CancellationToken cancellationToken)
    {
        await _questionService.DeleteAsync(User.GetUserId(), questionId, cancellationToken);
        return NoContent();
    }
}
