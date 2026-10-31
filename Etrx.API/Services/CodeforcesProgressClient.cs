using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Etrx.Application.Constants;

namespace Etrx.API.Services;

public sealed class CodeforcesProgressClient(HttpClient http)
{
    private static readonly SemaphoreSlim RequestGate = new(1, 1);
    private static DateTimeOffset nextRequestAt;
    public const int PageSize = 1000;

    public async Task<Dictionary<(int ContestId, string Index), ProgressFacts>> GetHistoryAsync(
        string handle, CancellationToken ct)
    {
        var facts = new Dictionary<(int, string), ProgressFacts>();
        for (var from = 1; ; from += PageSize)
        {
            await RequestGate.WaitAsync(ct);
            Response? response;
            try
            {
                var delay = nextRequestAt - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
                var url = $"https://codeforces.com/api/user.status?handle={Uri.EscapeDataString(handle)}&from={from}&count={PageSize}";
                response = await http.GetFromJsonAsync<Response>(url, ct);
            }
            finally
            {
                nextRequestAt = DateTimeOffset.UtcNow.AddMilliseconds(2100);
                RequestGate.Release();
            }
            if (response?.Status != "OK" || response.Result == null)
                throw new HttpRequestException("Codeforces did not return a complete submission page.");
            foreach (var submission in response.Result)
            {
                var p = submission.Problem;
                if (p == null || p.ContestId <= 0 || string.IsNullOrEmpty(p.Index)) continue;
                var key = (p.ContestId, p.Index);
                facts.TryGetValue(key, out var old);
                facts[key] = new ProgressFacts(old.IsSolved || submission.Verdict == "OK",
                    old.HasFailedAttempt || ProblemStatuses.FailedVerdicts.Contains(submission.Verdict ?? ""));
            }
            // Do not stop at the first page: old accepted submissions matter too.
            if (response.Result.Count < PageSize) return facts;
        }
    }

    public readonly record struct ProgressFacts(bool IsSolved, bool HasFailedAttempt);
    public sealed class Response
    {
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("result")] public List<Submission>? Result { get; set; }
    }
    public sealed class Submission
    {
        [JsonPropertyName("verdict")] public string? Verdict { get; set; }
        [JsonPropertyName("problem")] public Problem? Problem { get; set; }
    }
    public sealed class Problem
    {
        [JsonPropertyName("contestId")] public int ContestId { get; set; }
        [JsonPropertyName("index")] public string Index { get; set; } = "";
    }
}
