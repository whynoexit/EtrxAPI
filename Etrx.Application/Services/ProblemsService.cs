using AutoMapper;
using Etrx.Application.Constants;
using Etrx.Application.Dtos.Problems;
using Etrx.Application.Exceptions.BadRequest;
using Etrx.Application.Interfaces;
using Etrx.Application.Providers;
using Etrx.Application.Queries.Common;
using Etrx.Application.Repositories.UnitOfWork;
using Etrx.Application.Specifications;
using Etrx.Domain.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Etrx.Application.Services;

public class ProblemsService : IProblemsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProblemsService(
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    
    public async Task<List<ProblemResponseDto>> GetProblemsByContestIdAsync(int contestId, string lang)
    {
        if (!Languages.GetAll().Contains(lang))
        {
            throw new InvalidLanguageException();
        }

        var problems = await _unitOfWork.Problems.GetByContestIdAsync(contestId);
        var response = _mapper.Map<List<ProblemResponseDto>>(problems, opts =>
        {
            opts.Items["lang"] = lang;
        });

        return response;
    }

    public async Task<ProblemWithPropsResponseDto> GetProblemsByPageWithSortAndFilterAsync(GetSortProblemRequestDto dto)
    {
        var spec = new ProblemsSpecification(dto);

        var pagedResult = await _unitOfWork.Problems.GetPagedAsync<ProblemResponseDto>(
            spec,
            new PaginationQueryParameters(dto.Pagination.Page, dto.Pagination.PageSize),
            dto.Lang, dto.CurrentUserId, dto.Statuses, dto.HideSolved);

        return new ProblemWithPropsResponseDto
        (
            Problems: _mapper.Map<List<ProblemResponseDto>>(pagedResult.Items), 
            Properties: SortingFieldsProvider.GetSortFields<ProblemResponseDto>(),
            PageCount: pagedResult.TotalPagesCount
        );
    }

    public async Task<GetProblemFiltersResponseDto> GetProblemFiltersAsync(GetSortProblemRequestDto dto)
    {
        var emptyDto = new GetSortProblemRequestDto { Filters = new ProblemFiltersDto() };
        var totalBounds = await _unitOfWork.Problems.GetFiltersAsync(_unitOfWork.Problems.GetFilteredQuery(emptyDto));

        var contextFilters = dto.Filters with
        {
            AvailableTags = [],
            AvailableIndexes = [],
            AvailableDivisions = [],
            AvailableRanks = []
        };

        var availableTags = await _unitOfWork.Problems.GetFilteredQuery(dto)
            .SelectMany(p => p.Tags)
            .GroupBy(t => t.Name)
            .Select(g => new
            {
                Name = g.Key,
                Priority = g.Max(t => t.Priority) 
            })
            .OrderByDescending(x => x.Priority) 
            .ThenBy(x => x.Name)
            .Select(x => x.Name)
            .ToListAsync();

        var idxQueryDto = dto with { Filters = dto.Filters with { AvailableIndexes = [] } };
        var availableIndexes = await _unitOfWork.Problems.GetFilteredQuery(idxQueryDto)
            .Select(p => p.Index)
            .Distinct()
            .ToListAsync();

        var divQueryDto = dto with { Filters = dto.Filters with { AvailableDivisions = [] } };
        var availableDivisions = await _unitOfWork.Problems.GetFilteredQuery(divQueryDto)
            .Where(p => p.Contest != null && !string.IsNullOrEmpty(p.Contest.Division))
            .Select(p => p.Contest.Division)
            .Distinct()
            .ToListAsync();

        var ranksQueryDto = dto with { Filters = dto.Filters with { AvailableRanks = [] } };
        var rawRatingsForRanks = await _unitOfWork.Problems.GetFilteredQuery(ranksQueryDto)
            .Select(p => p.Rating)
            .Distinct()
            .ToListAsync();
        var availableRanks = rawRatingsForRanks.Select(r => ProblemExpressions.GetRank(r)).Distinct().ToList();

        var numericBoundsDto = dto with
        {
            Filters = dto.Filters with
            {
                MinRating = null,
                MaxRating = null,
                MinPoints = null,
                MaxPoints = null,
                MinSolved = null,
                MaxSolved = null,
                MinDifficulty = null,
                MaxDifficulty = null
            }
        };

        var shrunkenBounds = await _unitOfWork.Problems.GetFiltersAsync(_unitOfWork.Problems.GetFilteredQuery(numericBoundsDto));

        var userCurrent = shrunkenBounds with
        {
            AvailableTags = availableTags,
            AvailableIndexes = availableIndexes,
            AvailableDivisions = availableDivisions,
            AvailableRanks = availableRanks,
            MinRating = shrunkenBounds.MinRating,
            MaxRating = shrunkenBounds.MaxRating,
            MinPoints = shrunkenBounds.MinPoints,
            MaxPoints = shrunkenBounds.MaxPoints,
            MinSolved = shrunkenBounds.MinSolved,
            MaxSolved = shrunkenBounds.MaxSolved,
            MinDifficulty = shrunkenBounds.MinDifficulty,
            MaxDifficulty = shrunkenBounds.MaxDifficulty
        };

        return new GetProblemFiltersResponseDto
        {
            TotalBounds = totalBounds,
            CurrentFilters = userCurrent
        };
    }
}
