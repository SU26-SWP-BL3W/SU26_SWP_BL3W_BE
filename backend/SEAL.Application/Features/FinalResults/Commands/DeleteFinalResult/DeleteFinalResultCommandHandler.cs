using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.FinalResults.Commands.DeleteFinalResult
{
    public class DeleteFinalResultCommandHandler : IRequestHandler<DeleteFinalResultCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public DeleteFinalResultCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(DeleteFinalResultCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm FinalResult cần xóa
            var finalResult = await _unitOfWork.GetRepository<FinalResult>().GetByIdAsync(request.Id);
            if (finalResult == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Kết quả chung cuộc có ID '{request.Id}' không tồn tại.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isOwner = finalResult.CreatedBy == currentUserId;
            
            bool isCoordinator = false;
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(finalResult.RoundId);
            if (round != null)
            {
                isCoordinator = await _eventRoleChecker.HasRoleAsync(
                    currentUserId,
                    round.EventId,
                    new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                    cancellationToken);
            }

            if (!isAdmin && !isOwner && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa kết quả này.");
            }

            // 2. Xóa cứng
            await _unitOfWork.GetRepository<FinalResult>().DeleteAsync(finalResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


