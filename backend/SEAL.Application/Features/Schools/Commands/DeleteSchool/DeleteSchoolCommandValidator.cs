using FluentValidation;

namespace SEAL_Application.Features.Schools.Commands.DeleteSchool
{
    public class DeleteSchoolCommandValidator : AbstractValidator<DeleteSchoolCommand>
    {
        public DeleteSchoolCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("ID không được để trống");
        }
    }
}
