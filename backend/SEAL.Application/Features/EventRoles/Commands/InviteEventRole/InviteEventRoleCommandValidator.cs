using FluentValidation;
using SEAL_Domain.Entity.Enums;

namespace SEAL_Application.Features.EventRoles.Commands.InviteEventRole
{
    public class InviteEventRoleCommandValidator : AbstractValidator<InviteEventRoleCommand>
    {
        public InviteEventRoleCommandValidator()
        {
            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu lời mời không được để trống.");

            RuleFor(x => x.Model.EventId)
                .NotEmpty().WithMessage("EventId không được để trống.")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.InvitedUserId)
                .NotEmpty().WithMessage("Người được mời (InvitedUserId) không được để trống.")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.RoleName)
                .Must(role => role == EventRoleType.Judge
                           || role == EventRoleType.Mentor
                           || role == EventRoleType.EventCoordinator)
                .WithMessage("Chỉ được mời các vai trò Judge, Mentor hoặc EventCoordinator.")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.Notes)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự.")
                .When(x => x.Model != null && !string.IsNullOrEmpty(x.Model.Notes));
        }
    }
}
