using Etrx.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Etrx.API.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/ioiCodeforces")]
public class IoiCodeforcesController : ControllerBase
{
    private readonly IUpdateDataService _updateDataService;

    public IoiCodeforcesController(IUpdateDataService updateDataService)
    {
        _updateDataService = updateDataService;
    }

    [HttpPost("contests")]
    public async Task<IActionResult> GetContestsAsync()
    {
        await _updateDataService.UpdateIoiContestsAsync();

        return Ok();
    }

    [HttpPost("ranklistRows/{contestId:int}")]
    public async Task<IActionResult> GetStandingsAsync([FromRoute] int contestId)
    {
        await _updateDataService.UpdateIoiRanklistRowsByContestIdAsync(contestId);

        return Ok();
    }
}
