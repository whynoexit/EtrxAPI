using Etrx.Application.Constants;
using Etrx.Persistence.Databases;
using Microsoft.EntityFrameworkCore;

namespace Etrx.Persistence.Queries;

public sealed class ProblemProgressRow
{
    public int ContestId { get; set; }
    public string Index { get; set; } = string.Empty;
    public string Status { get; set; } = ProblemStatuses.None;
    public bool IsDeferred { get; set; }
}

public static class ProblemProgressQuery
{
    public static IQueryable<ProblemProgressRow> ForUser(EtrxDbContext db, Guid userId)
    {
        // Include the existing protocol immediately, including future background imports.
        var saved = db.UserProblemProgress.AsNoTracking().Where(p => p.UserId == userId)
            .Select(p => new { p.ContestId, p.Index, p.IsSolved, p.HasFailedAttempt, p.IsDeferred });
        var protocol = db.Submissions.AsNoTracking().Where(s => s.UserId == userId)
            .Select(s => new
            {
                s.ContestId, s.Index,
                IsSolved = s.Verdict == Verdicts.Ok,
                HasFailedAttempt = s.Verdict != null && ProblemStatuses.FailedVerdicts.Contains(s.Verdict),
                IsDeferred = false
            });
        return saved.Concat(protocol).GroupBy(p => new { p.ContestId, p.Index })
            .Select(g => new ProblemProgressRow
            {
                ContestId = g.Key.ContestId,
                Index = g.Key.Index,
                IsDeferred = g.Any(p => p.IsDeferred),
                Status = g.Any(p => p.IsSolved) ? ProblemStatuses.Solved
                    : g.Any(p => p.IsDeferred) ? ProblemStatuses.Deferred
                    : g.Any(p => p.HasFailedAttempt) ? ProblemStatuses.Failed : ProblemStatuses.None
            });
    }
}
