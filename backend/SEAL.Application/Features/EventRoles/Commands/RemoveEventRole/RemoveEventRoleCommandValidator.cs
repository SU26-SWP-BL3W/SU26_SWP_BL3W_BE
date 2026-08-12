using FluentValidation;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Entity;
using SEAL_Domain.Entity.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.EventRoles.Commands.RemoveEventRole
{
    public class RemoveEventRoleCommandValidator : AbstractValidator<RemoveEventRoleCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public RemoveEventRoleCommandValidator(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID không được để trống");

            // Logic: Chỉ Admin mới có quyền xóa vai trò EventCoordinator
            RuleFor(x => x.Id)
                .CustomAsync(async (id, context, cancellationToken) =>
                {
                    if (string.IsNullOrEmpty(id)) return;

                    var eventRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(id);
                    if (eventRole == null) return; // Handler will handle 404

                    if (eventRole.RoleName == EventRoleType.EventCoordinator)
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
                            context.AddFailure("Chỉ Admin hệ thống mới có quyền xóa hoặc thu hồi vai trò Điều phối viên sự kiện (Event Coordinator).");
                        }
                    }
                });
        }
    }
}
