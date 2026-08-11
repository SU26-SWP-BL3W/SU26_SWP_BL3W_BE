using FluentValidation;

namespace SEAL_Application.Features.Teams.Queries.GetMySubmissions
{
    public class GetMySubmissionsQueryValidator : AbstractValidator<GetMySubmissionsQuery>
    {
        public GetMySubmissionsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("PageNumber phải lớn hơn hoặc bằng 1");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("PageSize phải lớn hơn hoặc bằng 1");
        }
    }
}
