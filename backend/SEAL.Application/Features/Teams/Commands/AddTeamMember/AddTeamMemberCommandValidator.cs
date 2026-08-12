using FluentValidation;

namespace SEAL_Application.Features.Teams.Commands.AddTeamMember
{
    public class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
    {
        public AddTeamMemberCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("ID nhóm không được để trống.");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu không được để trống.");

            RuleFor(x => x.Model.UserId)
                .NotEmpty().WithMessage("ID người dùng cần thêm vào nhóm không được để trống.")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Notes)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.Notes));
        }
    }
}
