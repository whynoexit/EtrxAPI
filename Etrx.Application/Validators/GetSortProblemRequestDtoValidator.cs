using Etrx.Application.Constants;
using Etrx.Application.Dtos.Problems;
using Etrx.Application.Providers;
using FluentValidation;

namespace Etrx.Application.Validators;

public class GetSortProblemRequestDtoValidator : AbstractValidator<GetSortProblemRequestDto>
{
    private const int MAX_PAGE_SIZE = 100;

    public GetSortProblemRequestDtoValidator()
    {
        RuleForEach(x => x.Statuses).Must(ProblemStatuses.Filters.Contains)
            .WithMessage("Status must be solved, failed or deferred");
        var allowedSortFields = SortingFieldsProvider.GetSortFields<ProblemResponseDto>();

        RuleFor(x => x.Pagination.Page)
            .NotEmpty().WithMessage("Page can not be empty")
            .GreaterThan(0).WithMessage("Page number must be greater than zero");

        RuleFor(x => x.Pagination.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than zero")
            .LessThanOrEqualTo(MAX_PAGE_SIZE).WithMessage($"Page size must not exceed {MAX_PAGE_SIZE}");

        RuleFor(x => x.Sorting.SortField.ToLower())
            .NotEmpty().WithMessage("SortField can not be empty")
            .Must(allowedSortFields.Contains).WithMessage($"SortField must be one of: {string.Join(", ", allowedSortFields)}");

        RuleFor(x => x.Sorting.SortOrder)
            .NotNull().WithMessage("SortOrder can not be empty")
            .Must(SortOrders.GetAll().Contains).WithMessage($"SortOrder must be one of: {string.Join(", ", SortOrders.GetAll())}");

        RuleFor(x => x.IsOnly)
            .NotNull().WithMessage("IsOnly can not be empty");

        RuleFor(x => x.Lang)
            .NotEmpty().WithMessage("Lang can not be empty")
            .Must(Languages.GetAll().Contains).WithMessage($"Lang must be one of: {string.Join(", ", Languages.GetAll())}");
    }
}
