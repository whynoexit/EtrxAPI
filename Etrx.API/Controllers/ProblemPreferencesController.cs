using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Etrx.Persistence.Databases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etrx.API.Controllers;

[ApiController]
[Authorize]
[Route("api/problems/preferences")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProblemPreferencesController(EtrxDbContext db) : ControllerBase
{
    private static readonly HashSet<string> AllowedKeys = new(StringComparer.Ordinal)
    {
        "tags", "indexes", "divisions", "ranks", "minRating", "maxRating",
        "minPoints", "maxPoints", "minSolved", "maxSolved", "minDifficulty",
        "maxDifficulty", "problemName", "isOnly", "sortField", "sortOrder",
        "statuses", "hideSolved"
    };

    public record SaveRequest(
        [Required] Dictionary<string, string>? Filters,
        [Required, StringLength(100)] string ExpectedHandle);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var json = await db.UserProblemPreferences.AsNoTracking()
            .Where(p => p.UserId == userId).Select(p => p.FiltersJson)
            .SingleOrDefaultAsync(cancellationToken);
        var filters = new Dictionary<string, string>();
        if (json != null)
        {
            try { filters = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? []; }
            catch (JsonException) { /* Invalid legacy data must not block the problem list. */ }
        }
        return Ok(new { filters });
    }

    [HttpPut]
    [RequestSizeLimit(32768)]
    public async Task<IActionResult> Save(SaveRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        // A delayed request from another login must never write this account's settings.
        if (!string.Equals(User.Identity?.Name, request.ExpectedHandle, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Аккаунт изменился. Фильтры не были сохранены." });
        if (request.Filters == null || request.Filters.Count > AllowedKeys.Count ||
            request.Filters.Any(p => !AllowedKeys.Contains(p.Key) || p.Value == null || p.Value.Length > 4096))
            return BadRequest(new { message = "Некорректные фильтры." });

        var filters = request.Filters.Where(p => !string.IsNullOrEmpty(p.Value))
            .ToDictionary(p => p.Key, p => p.Value);
        var json = JsonSerializer.Serialize(filters);
        // One atomic upsert also handles simultaneous first saves from two tabs.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_problem_preferences (user_id, filters_json)
            VALUES ({userId}, {json})
            ON CONFLICT (user_id) DO UPDATE SET filters_json = EXCLUDED.filters_json
            """, cancellationToken);
        return NoContent();
    }
}
