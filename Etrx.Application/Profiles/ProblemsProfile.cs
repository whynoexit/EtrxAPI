using AutoMapper;
using Etrx.Application.Constants;
using Etrx.Application.Dtos.Problems;
using Etrx.Domain.Expressions;
using Etrx.Domain.Models;
using Etrx.Domain.Models.ParsingModels.Codeforces;

namespace Etrx.Application.Profiles;

public class ProblemsProfile : Profile
{
    public ProblemsProfile()
    {
        string lang = Languages.Ru;

        CreateMap<Problem, ProblemResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeferred, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src =>
                src.ProblemTranslations
                   .Where(pt => pt.LanguageCode == lang)
                   .Select(pt => pt.Name)
                   .FirstOrDefault() ?? "Unnamed Problem"))
            .AfterMap((src, dest, context) =>
            {
                if (context.Items.TryGetValue("lang", out var langObj) && langObj is string langValue)
                {
                    dest.Name = src.ProblemTranslations?
                        .FirstOrDefault(ct => ct.LanguageCode == langValue)?.Name ?? "Unnamed Problem";
                }
            })
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src =>
                src.Contest.StartTime))
            .ForMember(dest => dest.Difficulty, opt => opt.MapFrom(ProblemExpressions.DifficultyExpression))
            .ForMember(dest => dest.SolvedCount, opt => opt.MapFrom(src => src.SolvedCount))
            .ForMember(dest => dest.Rank, opt => opt.MapFrom(src => ProblemExpressions.GetRank(src.Rating)))
            .ForMember(dest => dest.Division, opt => opt.MapFrom(src => src.Contest.Division))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.OrderByDescending(t => t.Priority).Select(t => t.Name)));

        CreateMap<CodeforcesProblem, Problem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SolvedCount, opt => opt.Ignore())
            .ForMember(dest => dest.ProblemTranslations, opt => opt.Ignore())
            .ForMember(dest => dest.GuidContestId, opt => opt.Ignore())
            .ForMember(dest => dest.Tags, opt => opt.Ignore());

        CreateMap<CodeforcesProblem, ProblemTranslation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ProblemId, opt => opt.Ignore())
            .ForMember(dest => dest.LanguageCode, opt => opt.Ignore());
    }
}
