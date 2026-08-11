using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.Tracks.Commands.DeleteTrack
{
    public class DeleteTrackCommandHandler : IRequestHandler<DeleteTrackCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public DeleteTrackCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(DeleteTrackCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Track cần xóa
            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(request.Id);
            if (track == null)
            {
                return BaseException.BadRequestNotFoundResponse($"Hạng mục có ID '{request.Id}' không tồn tại.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isOwner = track.CreatedBy == currentUserId;
            
            bool isCoordinator = false;
            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(track.RoundId);
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
                return new BaseException.ForbiddenException("Bạn không có quyền xóa hạng mục này.");
            }

            // 1b. Hạng mục đã có bài nộp -> KHÔNG xóa: bài đã chấm sẽ nổ lỗi FK thô
            //     (Score -> SubmitResult là Restrict), bài chưa chấm sẽ bị cascade xóa im lặng.
            var hasSubmissions = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                sr => sr.TrackId == track.Id, cancellationToken);
            if (hasSubmissions)
            {
                return BaseException.BadRequestInvaildInputResponse("Hạng mục đã có bài nộp nên không thể xóa.");
            }

            // 2. Xóa mềm thực thể
            await _unitOfWork.GetRepository<Track>().DeleteAsync(track);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


