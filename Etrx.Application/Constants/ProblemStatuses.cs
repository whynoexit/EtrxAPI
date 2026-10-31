namespace Etrx.Application.Constants;

public static class ProblemStatuses
{
    public const string None = "none";
    public const string Solved = "solved";
    public const string Failed = "failed";
    public const string Deferred = "deferred";
    public static readonly string[] Filters = [Solved, Failed, Deferred];

    // Pending, skipped and infrastructure verdicts are not incorrect solutions.
    public static readonly string[] FailedVerdicts =
    [
        "FAILED", "PARTIAL", "COMPILATION_ERROR", "RUNTIME_ERROR", "WRONG_ANSWER",
        "TIME_LIMIT_EXCEEDED", "MEMORY_LIMIT_EXCEEDED", "IDLENESS_LIMIT_EXCEEDED",
        "SECURITY_VIOLATED", "CRASHED", "CHALLENGED", "REJECTED"
    ];

    public static string Resolve(bool solved, bool failed, bool deferred) =>
        solved ? Solved : deferred ? Deferred : failed ? Failed : None;
}
