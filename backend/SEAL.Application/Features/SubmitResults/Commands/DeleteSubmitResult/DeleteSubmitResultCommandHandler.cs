// [FLOW3-NOPBAI][DeleteSubmitResult] Doi thu hoi bai da nop truoc han chot, truoc khi co diem cham.

using MediatR;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Commands.DeleteSubmitResult
{
    public class DeleteSubmitResultCommandHandler : IRequestHandler<DeleteSubmitResultCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public DeleteSubmitResultCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<bool>> Handle(DeleteSubmitResultCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<SubmitResult>();

            // Nhận diện thẳng bằng string
            var submitResult = await repo.GetByIdAsync(request.Id);
            if (submitResult == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Bài nộp không tồn tại.");
            }

            // Lấy Team để biết EventId
            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(submitResult.TeamId);
            if (team == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Nhóm nộp bài không tồn tại.");
            }

            // Kiểm tra Ownership / Quyền hạn
            var currentUserId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return new BaseException.UnauthorizedException("Không thể xác thực người dùng.");
            }

            var currentUser = await _unitOfWork.GetRepository<User>().GetByIdAsync(currentUserId);
            bool isAdmin = currentUser != null && currentUser.IsAdmin;

            bool isTeamLeader = await _unitOfWork.GetRepository<EventRole>().AnyAsync(
                er => er.UserId == currentUserId 
                      && er.TeamId == team.Id 
                      && er.RoleName == SEAL_Domain.Entity.Enums.EventRoleType.TeamLeader,
                cancellationToken);

            bool isCoordinator = await _eventRoleChecker.HasRoleAsync(
                currentUserId,
                team.EventId,
                new[] { SEAL_Domain.Entity.Enums.EventRoleType.EventCoordinator },
                cancellationToken);

            if (!isAdmin && !isTeamLeader && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền xóa bài nộp của nhóm này.");
            }

            // Không cho XÓA bài đã được giám khảo chấm: FK Score -> SubmitResult là Restrict,
            // xóa thẳng sẽ nổ lỗi DB thô; chặn sớm để trả thông báo rõ ràng.
            var hasScores = await _unitOfWork.GetRepository<Score>().AnyAsync(
                sc => sc.SubmitResultId == submitResult.Id, cancellationToken);
            if (hasScores)
            {
                return new BaseException.ForbiddenException("Bài nộp đã được giám khảo chấm nên không thể xóa.");
            }

            // Không cho XÓA bài nộp sau khi kết quả vòng đã được tính/công bố.
            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(submitResult.TrackId);
            if (track != null)
            {
                // HẠN NỘP: hết thời gian vòng thì bài "đóng băng" — không cho xóa
                // (tránh rút bài sau hạn; xóa xong cũng không thể nộp lại vì đã quá hạn).
                var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(track.RoundId);
                if (round != null)
                {
                    var nowUtc = DateTime.UtcNow;
                    if (nowUtc < round.StartDate || nowUtc > round.EndDate)
                    {
                        return BaseException.BadRequestInvaildInputResponse("Ngoài thời gian vòng thi nên không thể xóa bài nộp.");
                    }
                }

                var roundPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId == track.RoundId, cancellationToken);
                if (roundPublished)
                {
                    return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể xóa bài nộp.");
                }
            }

            repo.Delete(submitResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}


