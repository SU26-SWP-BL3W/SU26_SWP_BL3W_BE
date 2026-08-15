using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Scores.Commands.DeleteScore
{
    public class DeleteScoreCommandHandler : IRequestHandler<DeleteScoreCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public DeleteScoreCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(DeleteScoreCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Score cần xóa
            var score = await _unitOfWork.GetRepository<Score>().GetByIdAsync(request.Id);
            if (score == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Phiếu chấm có ID '{request.Id}' không tồn tại.");
            }

            // Lấy EventRole của phiếu chấm
            var eventRole = await _unitOfWork.GetRepository<EventRole>().GetByIdAsync(score.EventRoleId);
            if (eventRole == null)
            {
                return BaseException.BadRequestNotFoundResponse("Vai trò chấm điểm của phiếu chấm này không hợp lệ.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isOwnRole = eventRole.UserId == currentUserId;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                eventRole.EventId,
                new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isAdmin && !isOwnRole && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa phiếu chấm này.");
            }

            // Khóa xóa khi kết quả vòng đã được tính/công bố (tránh lệch kết quả đã công bố).
            var submit = await _unitOfWork.GetRepository<SubmitResult>().GetByIdAsync(score.SubmitResultId);
            if (submit != null)
            {
                var published = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId == submit.RoundId, cancellationToken);
                if (published)
                    return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể xóa phiếu chấm.");
            }

            // 2. Xóa cứng thực thể (Cascade delete các ScoreDetail liên quan sẽ tự động chạy trong DB)
            await _unitOfWork.GetRepository<Score>().DeleteAsync(score);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


