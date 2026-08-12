using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.InviteTeamMember
{
    public class InviteTeamMemberCommandValidator : AbstractValidator<InviteTeamMemberCommand>
    {
        public InviteTeamMemberCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("ID nhóm không được để trống.");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu lời mời không được để trống.");

            RuleFor(x => x.Model.Email)
                .NotEmpty().WithMessage("Email người dùng được mời không được để trống.")
                .EmailAddress().WithMessage("Email không đúng định dạng.")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Notes)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.Notes));
        }
    }
}
