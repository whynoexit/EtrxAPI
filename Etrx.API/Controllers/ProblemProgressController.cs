using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Etrx.API.Services;
using Etrx.Persistence.Databases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etrx.API.Controllers;

[ApiController, Authorize, Route("api/problems/progress")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProblemProgressController(ProblemProgressService progress, EtrxDbContext db) : ControllerBase
{
    public record DeferredRequest(bool Deferred);

    [HttpPut("{contestId:int}/{index}/deferred")]
    public async Task<IActionResult> SetDeferred(int contestId, [StringLength(20, MinimumLength = 1)] string index,
        DeferredRequest request, CancellationToken ct)
    {
        if (contestId <= 0) return BadRequest();
        if (!await db.Problems.AnyAsync(p => p.ContestId == contestId && p.Index == index, ct)) return NotFound();
        await progress.SetDeferredAsync(UserId, contestId, index, request.Deferred, ct);
        return NoContent();
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken ct) =>
        Ok(await progress.SyncAsync(UserId, User.Identity!.Name!, ct));

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
