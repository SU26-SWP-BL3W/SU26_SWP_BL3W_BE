using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Events.Commands.DeleteEvent
{
    public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public DeleteEventCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm sự kiện
            var eventEntity = await _unitOfWork.GetRepository<Event>().GetByIdAsync(request.Id);
            if (eventEntity == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Sự kiện có ID '{request.Id}' không tồn tại.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isOwner = eventEntity.CreatedBy == currentUserId;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                eventEntity.Id,
                new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isAdmin && !isOwner && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa sự kiện này.");
            }

            // Thực hiện kiểm tra tính hợp lệ trước khi xóa
            var validationResult = await DeleteEventValidation.ValidateDeleteAsync(_unitOfWork, eventEntity, cancellationToken);
            if (validationResult.IsFailure)
            {
                return new BaseException.ErrorException(validationResult.StatusCode, validationResult.Error!);
            }

            // 2. Xóa sự kiện
            await _unitOfWork.GetRepository<Event>().DeleteAsync(eventEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


