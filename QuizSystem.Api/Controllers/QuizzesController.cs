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
public class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizService;

    public QuizzesController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Instructor)]
    public async Task<ActionResult<IReadOnlyCollection<QuizDto>>> Mine(CancellationToken cancellationToken)
    {
        var quizzes = await _quizService.GetInstructorQuizzesAsync(User.GetUserId(), cancellationToken);
        return Ok(quizzes);
    }

    [HttpGet("all")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<QuizDto>>> All(CancellationToken cancellationToken)
    {
        return Ok(await _quizService.GetAllQuizzesAsync(cancellationToken));
    }

    [HttpGet("assigned")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<IReadOnlyCollection<QuizDto>>> Assigned(CancellationToken cancellationToken)
    {
        return Ok(await _quizService.GetAssignedQuizzesForStudentAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("{quizId:guid}")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Instructor)]
    public async Task<ActionResult<QuizDto>> GetById(Guid quizId, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(AppRoles.Admin);
        var quiz = await _quizService.GetByIdAsync(User.GetUserId(), quizId, isAdmin, cancellationToken);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<ActionResult<QuizDto>> Create([FromBody] QuizCreateUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _quizService.CreateAsync(User.GetUserId(), request, cancellationToken));
    }

    [HttpPut("{quizId:guid}")]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<ActionResult<QuizDto>> Update(Guid quizId, [FromBody] QuizCreateUpdateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _quizService.UpdateAsync(User.GetUserId(), quizId, request, cancellationToken));
    }

    [HttpDelete("{quizId:guid}")]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<IActionResult> Delete(Guid quizId, CancellationToken cancellationToken)
    {
        await _quizService.DeleteAsync(User.GetUserId(), quizId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{quizId:guid}/publish")]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<IActionResult> Publish(Guid quizId, CancellationToken cancellationToken)
    {
        await _quizService.PublishAsync(User.GetUserId(), quizId, cancellationToken);
        return NoContent();
    }

    [HttpPost("assign")]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<IActionResult> Assign([FromBody] AssignQuizRequest request, CancellationToken cancellationToken)
    {
        await _quizService.AssignAsync(User.GetUserId(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("groups")]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<ActionResult<GroupClassDto>> CreateGroup([FromBody] GroupClassCreateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _quizService.CreateGroupAsync(User.GetUserId(), request, cancellationToken));
    }

    [HttpGet("groups")]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<GroupClassDto>>> GetGroups(CancellationToken cancellationToken)
    {
        return Ok(await _quizService.GetGroupsAsync(User.GetUserId(), cancellationToken));
    }
}
