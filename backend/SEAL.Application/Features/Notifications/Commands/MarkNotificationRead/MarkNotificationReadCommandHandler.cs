using MediatR;
using SEAL_Application.Interfaces;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Notifications.Commands.MarkNotificationRead
{
    public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public MarkNotificationReadCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng. Vui lòng đăng nhập.");
            }

            var item = await _unitOfWork.GetRepository<AppNotification>().GetByIdAsync(request.NotificationId);
            if (item == null || item.UserId != userId)
            {
                return BaseException.BadRequestNotFoundResponse("Thông báo không tồn tại.");
            }

            item.IsRead = true;
            await _unitOfWork.GetRepository<AppNotification>().UpdateAsync(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
