using Etrx.Application.Dtos.Common;
using Etrx.Application.Dtos.Problems;
using Etrx.Application.Queries.Common;
using Etrx.Application.Specifications;
using Etrx.Domain.Models;

namespace Etrx.Application.Repositories;

public interface IProblemsRepository : IGenericRepository<Problem>
{
    new Task<List<Problem>> GetAllAsync();
    Task<Problem?> GetByContestIdAndIndexAsync(int contestId, string index);
    Task<List<Problem>> GetByContestIdAsync(int contestId);
    Task<List<string>> GetIndexesByContestIdAsync(int contestId);
    Task<PagedResultDto<TResult>> GetPagedAsync<TResult>(BaseSpecification<Problem> spec, PaginationQueryParameters pagination, string lang, Guid? userId = null, string[]? statuses = null, bool hideSolved = false);
    Task<List<Problem>> GetByContestAndIndexAsync(List<(int ContestId, string Index)> identifiers);
    IQueryable<Problem> GetFilteredQuery(GetSortProblemRequestDto dto);
    Task<ProblemFiltersDto> GetFiltersAsync(IQueryable<Problem> query);
    Task<ProblemFiltersDto> GetAbsoluteBoundsAsync();
}
