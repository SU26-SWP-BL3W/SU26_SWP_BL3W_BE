using FluentValidation;

namespace SEAL_Application.Features.Users.Commands.UpdateStudentProfile
{
    public class UpdateStudentProfileCommandValidator : AbstractValidator<UpdateStudentProfileCommand>
    {
        public UpdateStudentProfileCommandValidator()
        {
            RuleFor(x => x.SchoolId)
                .NotEmpty().WithMessage("Trường học (SchoolId) không được để trống.");

            RuleFor(x => x.StudentCode)
                .NotEmpty().WithMessage("Mã sinh viên là bắt buộc đối với sinh viên FPT University.")
                .When(x => x.IsFpt);

            RuleFor(x => x.PhotoStudentCardUrl)
                .NotEmpty().WithMessage("Ảnh thẻ sinh viên là bắt buộc đối với sinh viên ngoài FPT University.")
                .When(x => !x.IsFpt);
        }
    }
}
