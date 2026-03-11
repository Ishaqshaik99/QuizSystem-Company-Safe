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
public class ResultsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ResultsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("student/dashboard")]
    [Authorize(Roles = AppRoles.Student)]
    public async Task<ActionResult<StudentDashboardDto>> StudentDashboard(CancellationToken cancellationToken)
    {
        return Ok(await _reportService.GetStudentDashboardAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("quiz/{quizId:guid}/analytics")]
    [Authorize(Roles = AppRoles.Instructor + "," + AppRoles.Admin)]
    public async Task<ActionResult<InstructorQuizAnalyticsDto>> QuizAnalytics(Guid quizId, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole(AppRoles.Admin);
        var result = await _reportService.GetQuizAnalyticsAsync(User.GetUserId(), quizId, isAdmin, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/overview")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<AdminOverviewDto>> AdminOverview(CancellationToken cancellationToken)
    {
        return Ok(await _reportService.GetAdminOverviewAsync(cancellationToken));
    }
}
