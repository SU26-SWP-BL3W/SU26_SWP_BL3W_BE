using FluentValidation;
using SEAL_Domain.Entity.Enums;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Commands.UpdateEventRole
{
    public class UpdateEventRoleCommandValidator : AbstractValidator<UpdateEventRoleCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public UpdateEventRoleCommandValidator(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID không được để trống");

            RuleFor(x => x.Model)
                .NotNull().WithMessage("Dữ liệu không được để trống");

            RuleFor(x => x.Model.RoleName)
                .IsInEnum().WithMessage("RoleName không hợp lệ")
                .When(x => x.Model != null);
                
            RuleFor(x => x.Model.TeamId)
                .NotEmpty().WithMessage("TeamId là bắt buộc đối với TeamLeader hoặc TeamMember")
                .When(x => x.Model != null && (x.Model.RoleName == EventRoleType.TeamLeader || x.Model.RoleName == EventRoleType.TeamMember));

            // Logic: Chỉ Admin mới có quyền thay đổi hoặc gán vai trò EventCoordinator, đồng thời kiểm tra xung đột vai trò
            RuleFor(x => x)
                .CustomAsync(async (command, context, cancellationToken) =>
                {
                    if (command.Model == null) return;

                    var eventRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(command.Id);
                    if (eventRole == null) return; // Handler will handle 404

                    if (eventRole.RoleName == EventRoleType.EventCoordinator || command.Model.RoleName == EventRoleType.EventCoordinator)
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
                            context.AddFailure("Chỉ Admin hệ thống mới có quyền thay đổi hoặc gán vai trò Điều phối viên sự kiện (Event Coordinator).");
                        }
                    }

                    // Kiểm tra xung đột vai trò (Role Conflict Validation)
                    var conflictError = await EventRoleValidationHelper.CheckRoleConflictAsync(
                        _unitOfWork,
                        eventRole.UserId,
                        eventRole.EventId,
                        command.Model.RoleName,
                        command.Model.TrackId,
                        command.Id, // Update thì loại trừ ID đang cập nhật
                        cancellationToken);

                    if (conflictError != null)
                    {
                        context.AddFailure(conflictError);
                    }
                });
        }
    }
}
