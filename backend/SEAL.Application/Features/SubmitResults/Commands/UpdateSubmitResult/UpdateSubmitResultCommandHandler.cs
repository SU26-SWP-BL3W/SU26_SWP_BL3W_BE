// [FLOW3-NOPBAI][UpdateSubmitResult] Sua bai da nop trong luc Track con mo; chan sua sau khi vong thi da cong bo ket qua.


using MediatR;
using SEAL_Application.Features.SubmitResults.Commands.UpdateSubmitResult.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using SEAL_Domain.Ultis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEAL_Application.Features.SubmitResults.Commands.UpdateSubmitResult
{
    public class UpdateSubmitResultCommandHandler : IRequestHandler<UpdateSubmitResultCommand, Result<UpdateSubmitResultResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public UpdateSubmitResultCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<UpdateSubmitResultResponseModel>> Handle(UpdateSubmitResultCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<SubmitResult>();

            // Tìm kiếm thẳng bằng string (sử dụng request.Id thay vì request.Model.Id)
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

            if (!isTeamLeader && !isCoordinator)
            {
                return new BaseException.ForbiddenException("Bạn không có quyền cập nhật bài nộp của nhóm này.");
            }

            // Không cho SỬA bài đã được giám khảo chấm (đổi bài sau khi có điểm = sai lệch kết quả).
            var hasScores = await _unitOfWork.GetRepository<Score>().AnyAsync(
                sc => sc.SubmitResultId == submitResult.Id, cancellationToken);
            if (hasScores)
            {
                return new BaseException.ForbiddenException("Bài nộp đã được giám khảo chấm nên không thể sửa.");
            }

            // Không cho SỬA bài nộp sau khi kết quả vòng đã được tính/công bố (tránh đổi bài đã bị chấm).
            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(submitResult.TrackId);
            if (track != null)
            {
                // HẠN NỘP: chỉ được sửa bài khi hạng mục còn đang mở (ưu tiên Track, fallback Round).
                var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(track.RoundId);
                if (round != null)
                {
                    var nowUtc = DateTime.UtcNow;
                    var effectiveStartDate = track.StartDate ?? round.StartDate;
                    var effectiveEndDate = track.EndDate ?? round.EndDate;

                    if (nowUtc < effectiveStartDate || nowUtc > effectiveEndDate)
                    {
                        return BaseException.BadRequestInvaildInputResponse("Ngoài thời gian hạng mục mở nên không thể sửa bài nộp.");
                    }
                }

                var roundPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.RoundId == track.RoundId, cancellationToken);
                if (roundPublished)
                {
                    return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể sửa bài nộp.");
                }
            }

            // IsActive quyết định bài có được TÍNH KẾT QUẢ hay không -> chỉ Event Coordinator (hoặc Admin)
            // được thay đổi; đội tự đổi có thể vô tình tự loại mình khỏi vòng thi.
            // null = client không đụng tới IsActive -> giữ nguyên (tránh "tắt ngầm" khi quên gửi field).
            if (request.Model.IsActive.HasValue && request.Model.IsActive.Value != submitResult.IsActive && !isCoordinator)
            {
                return new BaseException.ForbiddenException(
                    "Chỉ Event Coordinator được thay đổi trạng thái hiệu lực (IsActive) của bài nộp.");
            }

            if (!string.IsNullOrWhiteSpace(request.Model.SubmissionUrl))
            {
                submitResult.SubmissionUrl = request.Model.SubmissionUrl;
            }
            if (request.Model.Description != null) // null = giữ mô tả cũ; "" = chủ đích xóa mô tả
            {
                submitResult.Description = request.Model.Description;
            }
            if (request.Model.IsActive.HasValue)
            {
                submitResult.IsActive = request.Model.IsActive.Value;
            }
            submitResult.LastUpdatedBy = currentUserId; // ghi lại AI sửa lần cuối
            submitResult.LastUpdatedTime = CoreHelper.SystemTimeNow; // Sử dụng LastUpdatedTime khớp cấu trúc BaseEntity

            repo.Update(submitResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateSubmitResultResponseModel
            {
                Id = submitResult.Id,
                TeamId = submitResult.TeamId,
                TrackId = submitResult.TrackId,
                SubmissionUrl = submitResult.SubmissionUrl,
                Description = submitResult.Description,
                IsActive = submitResult.IsActive,
                LastUpdatedTime = submitResult.LastUpdatedTime
            };
        }
    }
}

