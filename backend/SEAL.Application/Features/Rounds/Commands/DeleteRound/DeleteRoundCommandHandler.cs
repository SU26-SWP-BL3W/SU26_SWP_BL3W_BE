using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Rounds.Commands.DeleteRound
{
    public class DeleteRoundCommandHandler : IRequestHandler<DeleteRoundCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public DeleteRoundCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(DeleteRoundCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Round cần xóa
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.Id);
            if (round == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Vòng thi có ID '{request.Id}' không tồn tại.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isOwner = round.CreatedBy == currentUserId;
            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                round.EventId,
                new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isAdmin && !isOwner && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa vòng thi này.");
            }

            // 2. Chặn xóa nếu vòng thi đã có bài nộp — xóa thẳng sẽ cascade xóa luôn SubmitResult
            //    (và Score/FinalResult liên quan) không cảnh báo, mất dữ liệu chấm thi.
            var hasSubmissions = await _unitOfWork.GetRepository<SubmitResult>()
                .AnyAsync(sr => sr.RoundId == round.Id, cancellationToken);
            if (hasSubmissions)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Không thể xóa vòng thi đã có bài nộp. Vui lòng xóa/di chuyển các bài nộp trước.");
            }

            // 3. Xóa cứng thực thể (Cascade delete các Track liên quan sẽ tự động chạy trong DB)
            await _unitOfWork.GetRepository<Round>().DeleteAsync(round);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


