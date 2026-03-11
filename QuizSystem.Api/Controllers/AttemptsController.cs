using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizSystem.Api.Extensions;
using QuizSystem.Core.DTOs;
using QuizSystem.Core.Enums;
using QuizSystem.Core.Interfaces;

namespace QuizSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttemptsController : ControllerBase
{
    private readonly IAttemptService _attemptService;

    public AttemptsController(IAttemptService attemptService)
    {
        _attemptService = attemptService;
    }

    [HttpPost("start")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<AttemptSessionDto>> Start([FromBody] StartAttemptRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _attemptService.StartAttemptAsync(User.GetUserId(), request, cancellationToken));
    }

    [HttpPost("{attemptId:guid}/answers")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<IActionResult> SaveAnswer(Guid attemptId, [FromBody] SaveAnswerRequest request, CancellationToken cancellationToken)
    {
        await _attemptService.SaveAnswerAsync(User.GetUserId(), attemptId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{attemptId:guid}/session")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<AttemptSessionDto>> Session(Guid attemptId, CancellationToken cancellationToken)
    {
        var session = await _attemptService.GetActiveSessionAsync(User.GetUserId(), attemptId, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("{attemptId:guid}/submit")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<AttemptResultDto>> Submit(Guid attemptId, [FromBody] SubmitAttemptRequest request, CancellationToken cancellationToken)
    {
        var result = await _attemptService.SubmitAsync(User.GetUserId(), attemptId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{attemptId:guid}/auto-submit")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<AttemptResultDto>> AutoSubmit(Guid attemptId, CancellationToken cancellationToken)
    {
        var result = await _attemptService.SubmitAsync(User.GetUserId(), attemptId, new SubmitAttemptRequest { ForceSubmit = true }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("auto-submit-expired")]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<ActionResult<int>> AutoSubmitExpired(CancellationToken cancellationToken)
    {
        return Ok(await _attemptService.AutoSubmitExpiredAsync(cancellationToken));
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<IReadOnlyCollection<AttemptResultDto>>> Mine(CancellationToken cancellationToken)
    {
        return Ok(await _attemptService.GetStudentAttemptsAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("all")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<AttemptResultDto>>> All(CancellationToken cancellationToken)
    {
        return Ok(await _attemptService.GetAllAttemptsAsync(cancellationToken));
    }

    [HttpGet("quiz/{quizId:guid}")]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<AttemptResultDto>>> ByQuiz(Guid quizId, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(AppRoles.Admin);
        var items = await _attemptService.GetAttemptsByQuizAsync(User.GetUserId(), quizId, isAdmin, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{attemptId:guid}")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<AttemptResultDto>> GetById(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _attemptService.GetAttemptDetailForStudentAsync(User.GetUserId(), attemptId, cancellationToken);
        return attempt is null ? NotFound() : Ok(attempt);
    }

    [HttpPost("{attemptId:guid}/grade-short-answer")]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<ActionResult<AttemptResultDto>> GradeShortAnswer(Guid attemptId, [FromBody] ManualGradeRequest request, CancellationToken cancellationToken)
    {
        var graded = await _attemptService.GradeShortAnswerAsync(User.GetUserId(), attemptId, request, cancellationToken);
        return Ok(graded);
    }
}
