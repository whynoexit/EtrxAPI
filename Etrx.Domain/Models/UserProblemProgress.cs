namespace Etrx.Domain.Models;

// Codeforces identifiers stay stable when the catalogue is reimported.
public class UserProblemProgress
{
    public Guid UserId { get; set; }
    public int ContestId { get; set; }
    public string Index { get; set; } = string.Empty;
    public bool IsSolved { get; set; }
    public bool HasFailedAttempt { get; set; }
    public bool IsDeferred { get; set; }
}

public class UserProblemSync
{
    public Guid UserId { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
}
