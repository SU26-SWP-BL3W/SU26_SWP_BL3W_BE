using FluentValidation;

namespace SEAL_Application.Features.Users.Commands.RequestUnblock
{
    public class RequestUnblockCommandValidator : AbstractValidator<RequestUnblockCommand>
    {
        public RequestUnblockCommandValidator()
        {
            RuleFor(v => v.Model.Email)
                .NotEmpty().WithMessage("Email là bắt buộc.")
                .EmailAddress().WithMessage("Email không hợp lệ.");
        }
    }
}
