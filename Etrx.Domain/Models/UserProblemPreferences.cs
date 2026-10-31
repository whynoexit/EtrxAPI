namespace Etrx.Domain.Models;

// Kept separately from imported Codeforces profiles and problem progress.
public class UserProblemPreferences
{
    public Guid UserId { get; set; }
    public string FiltersJson { get; set; } = "{}";
}
