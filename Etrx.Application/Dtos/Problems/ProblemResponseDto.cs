using Etrx.Domain.Enums;

namespace Etrx.Application.Dtos.Problems;

public class ProblemResponseDto
{
    public string Status { get; set; } = "none";
    public bool IsDeferred { get; set; }
    public int ContestId { get; set; }
    public string Index { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? Points { get; set; }
    public int? Rating { get; set; }
    public List<string> Tags { get; set; } = [];
    public int SolvedCount { get; set; }
    public long StartTime { get; set; } 
    public int Difficulty { get; set; }
    public RankEnum? Rank { get; set; }
    public string? Division { get; set; } = string.Empty;
}
