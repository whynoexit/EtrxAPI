using Etrx.Application.Dtos.Problems;
using Etrx.Domain.Models;
using Etrx.Persistence.Databases;
using Etrx.Persistence.Queries;
using Microsoft.EntityFrameworkCore;

namespace Etrx.API.Services;

public sealed class ProblemProgressService(EtrxDbContext db, CodeforcesProgressClient codeforces,
    ILogger<ProblemProgressService> logger)
{
    public async Task EnrichAsync(Guid userId, IEnumerable<ProblemResponseDto> items, CancellationToken ct)
    {
        var problems = items.ToList();
        var contestIds = problems.Select(p => p.ContestId).Distinct().ToArray();
        if (contestIds.Length == 0) return;
        var rows = await ProblemProgressQuery.ForUser(db, userId)
            .Where(p => contestIds.Contains(p.ContestId)).ToListAsync(ct);
        var states = rows.ToDictionary(p => (p.ContestId, p.Index));
        foreach (var p in problems)
            if (states.TryGetValue((p.ContestId, p.Index), out var state))
            { p.Status = state.Status; p.IsDeferred = state.IsDeferred; }
    }

    public async Task SetDeferredAsync(Guid userId, int contestId, string index, bool deferred, CancellationToken ct)
    {
        // Atomic upsert changes only the manual flag; a concurrent sync cannot erase it.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_problem_progress (user_id, contest_id, index, is_solved, has_failed_attempt, is_deferred)
            VALUES ({userId}, {contestId}, {index}, FALSE, FALSE, {deferred})
            ON CONFLICT (user_id, contest_id, index) DO UPDATE SET is_deferred = EXCLUDED.is_deferred
            """, ct);
    }

    public async Task<SyncResult> SyncAsync(Guid userId, string handle, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Database lock works across API instances as well as concurrent browser tabs.
        var acquired = await db.Database.SqlQuery<bool>($"""
            SELECT pg_try_advisory_xact_lock(hashtextextended({userId.ToString()}, 0)) AS "Value"
            """).SingleAsync(ct);
        if (!acquired) return new(false, null, null, true);

        var sync = await db.UserProblemSync.FindAsync([userId], ct);
        if (sync == null) { sync = new UserProblemSync { UserId = userId }; db.Add(sync); }
        var now = DateTimeOffset.UtcNow;
        if (sync.LastAttemptAt > now.AddSeconds(-60))
            return new(sync.LastSuccessAt >= sync.LastAttemptAt, sync.LastSuccessAt,
                sync.LastSuccessAt >= sync.LastAttemptAt ? null :
                    "Codeforces временно недоступен. Показаны сохранённые результаты.", true);
        sync.LastAttemptAt = now;
        SyncResult result;
        try
        {
            // Fetch every page before writing. A failed later page must not erase cached results.
            var history = await codeforces.GetHistoryAsync(handle, ct);
            foreach (var chunk in history.Where(p => p.Value.IsSolved || p.Value.HasFailedAttempt).Chunk(500))
            {
                // Separate updates preserve deferred flags and keep accepted results monotonic.
                // Parameterised SQL also makes repeated imports idempotent.
                var contestIds = chunk.Select(p => p.Key.ContestId).ToArray();
                var indexes = chunk.Select(p => p.Key.Index).ToArray();
                var solved = chunk.Select(p => p.Value.IsSolved).ToArray();
                var failed = chunk.Select(p => p.Value.HasFailedAttempt).ToArray();
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO user_problem_progress (user_id, contest_id, index, is_solved, has_failed_attempt, is_deferred)
                    SELECT {userId}, p.contest_id, p.index, p.solved, p.failed, FALSE
                    FROM unnest({contestIds}::integer[], {indexes}::text[], {solved}::boolean[], {failed}::boolean[])
                        AS p(contest_id, index, solved, failed)
                    ON CONFLICT (user_id, contest_id, index) DO UPDATE SET
                        is_solved = user_problem_progress.is_solved OR EXCLUDED.is_solved,
                        has_failed_attempt = user_problem_progress.has_failed_attempt OR EXCLUDED.has_failed_attempt
                    """, ct);
            }
            sync.LastSuccessAt = DateTimeOffset.UtcNow;
            result = new(true, sync.LastSuccessAt, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException ||
            ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Could not synchronize problem progress for user {UserId}", userId);
            result = new(false, sync.LastSuccessAt,
                "Codeforces временно недоступен. Показаны сохранённые результаты.");
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public sealed record SyncResult(bool Success, DateTimeOffset? LastSuccessAt, string? Message, bool Skipped = false);
}
