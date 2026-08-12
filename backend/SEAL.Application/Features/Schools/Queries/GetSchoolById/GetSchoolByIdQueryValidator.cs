using FluentValidation;

namespace SEAL_Application.Features.Schools.Queries.GetSchoolById
{
    public class GetSchoolByIdQueryValidator : AbstractValidator<GetSchoolByIdQuery>
    {
        public GetSchoolByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("ID không được để trống");
        }
    }
}
