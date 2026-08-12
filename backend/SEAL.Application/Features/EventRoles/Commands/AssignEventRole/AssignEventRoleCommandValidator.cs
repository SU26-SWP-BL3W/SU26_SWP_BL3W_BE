using FluentValidation;
using SEAL_Domain.Entity.Enums;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Commands.AssignEventRole
{
    public class AssignEventRoleCommandValidator : AbstractValidator<AssignEventRoleCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public AssignEventRoleCommandValidator(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu không được để trống");

            RuleFor(x => x.Model.UserId)
                .NotEmpty().WithMessage("UserId không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.EventId)
                .NotEmpty().WithMessage("EventId không được để trống")
                .When(x => x.Model != null);

            RuleFor(x => x.Model.RoleName)
                .IsInEnum().WithMessage("RoleName không hợp lệ")
                .When(x => x.Model != null);
                
            RuleFor(x => x.Model.TeamId)
                .NotEmpty().WithMessage("TeamId là bắt buộc đối với TeamLeader hoặc TeamMember")
                .When(x => x.Model != null && (x.Model.RoleName == EventRoleType.TeamLeader || x.Model.RoleName == EventRoleType.TeamMember));

            // Logic: Chỉ Admin mới được gán vai trò EventCoordinator
            RuleFor(x => x.Model.RoleName)
                .CustomAsync(async (roleName, context, cancellationToken) =>
                {
                    if (roleName == EventRoleType.EventCoordinator)
                    {
                        var userId = _currentUserService.UserId;
                        if (string.IsNullOrEmpty(userId))
                        {
                            context.AddFailure("Người dùng chưa đăng nhập.");
                            return;
                        }

                        var user = await _unitOfWork.GetRepository<User>().GetByIdAsync(userId);
                        if (user == null || !user.IsAdmin)
                        {
                            context.AddFailure("Chỉ Admin hệ thống mới có quyền gán vai trò Điều phối viên sự kiện (Event Coordinator).");
                        }
                    }
                })
                .When(x => x.Model != null);

            // Kiểm tra xung đột vai trò (Role Conflict Validation)
            RuleFor(x => x.Model)
                .CustomAsync(async (model, context, cancellationToken) =>
                {
                    if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.EventId)) return;

                    var conflictError = await EventRoleValidationHelper.CheckRoleConflictAsync(
                        _unitOfWork,
                        model.UserId,
                        model.EventId,
                        model.RoleName,
                        model.TrackId,
                        null, // Assign thì không có ID cũ để loại trừ
                        cancellationToken);

                    if (conflictError != null)
                    {
                        context.AddFailure(conflictError);
                    }
                })
                .When(x => x.Model != null);
        }
    }
}
