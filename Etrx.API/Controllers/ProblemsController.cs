using Etrx.Application.Constants;
using Etrx.Application.Dtos.Problems;
using Etrx.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Etrx.API.Services;

namespace Etrx.API.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/problems")]
public class ProblemsController : ControllerBase
{
    private readonly IProblemsService _problemsService;
    private readonly ProblemProgressService _progress;

    public ProblemsController(IProblemsService problemsService, ProblemProgressService progress)
    {
        _problemsService = problemsService;
        _progress = progress;
    }

    [HttpGet("{contestId:int}")]
    public async Task<IActionResult> GetProblemsByContestIdAsync(
        [FromRoute] int contestId,
        [FromQuery] string lang = Languages.Ru)
    {
        return Ok(await _problemsService.GetProblemsByContestIdAsync(contestId, lang));
    }

    [HttpGet]
    public async Task<IActionResult> GetProblemsByPageWithSortAndFilterAsync(
        [FromQuery] GetSortProblemRequestDto dto)
    {
        dto.CurrentUserId = CurrentUserId();
        if ((dto.Statuses.Length > 0 || dto.HideSolved) && dto.CurrentUserId == null) return Unauthorized();
        var result = await _problemsService.GetProblemsByPageWithSortAndFilterAsync(dto);
        if (dto.CurrentUserId.HasValue)
            await _progress.EnrichAsync(dto.CurrentUserId.Value, result.Problems, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("filters")]
    public async Task<IActionResult> GetProblemFiltersAsync([FromQuery] GetSortProblemRequestDto dto)
    {
        dto.CurrentUserId = CurrentUserId();
        if ((dto.Statuses.Length > 0 || dto.HideSolved) && dto.CurrentUserId == null) return Unauthorized();
        return Ok(await _problemsService.GetProblemFiltersAsync(dto));
    }

    private Guid? CurrentUserId() => User.Identity?.IsAuthenticated == true &&
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
