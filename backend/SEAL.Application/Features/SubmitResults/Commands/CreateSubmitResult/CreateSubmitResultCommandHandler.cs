// [FLOW3-NOPBAI][CreateSubmitResult] Doi nop cac duong link (GitHub Repo, Demo, Slide) cho 1 Hang muc (Track) trong 1 Vong thi (Round).

using MediatR;
using Microsoft.EntityFrameworkCore;
using SEAL_Application.Features.SubmitResults.Commands.CreateSubmitResult.Models;
using SEAL_Application.Services.UnitOfWork;
using SEAL_Domain.Base;
using SEAL_Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Application.Features.SubmitResults.Commands.CreateSubmitResult
{
    public class CreateSubmitResultCommandHandler : IRequestHandler<CreateSubmitResultCommand, Result<CreateSubmitResultResponseModel>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SEAL_Application.Interfaces.ICurrentUserService _currentUserService;
        private readonly SEAL_Application.Interfaces.IEventRoleChecker _eventRoleChecker;

        public CreateSubmitResultCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
        }

        public async Task<Result<CreateSubmitResultResponseModel>> Handle(CreateSubmitResultCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra tồn tại khóa ngoại TeamId trong DB
            var team = await _unitOfWork.GetRepository<Team>().GetByIdAsync(request.Model.TeamId);
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
                return new BaseException.ForbiddenException("Bạn không có quyền nộp bài cho nhóm này.");
            }

            if (team.Status != SEAL_Domain.Entity.Enums.TeamStatus.Registered)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Đội chưa đăng ký chính thức (chưa chốt đủ thành viên/được duyệt) nên chưa thể nộp bài.");
            }

            // 2. Kiểm tra tồn tại Track & Round
            if (string.IsNullOrEmpty(request.Model.TrackId))
            {
                return BaseException.BadRequestInvaildInputResponse("TrackId không được để trống.");
            }

            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(request.Model.TrackId);
            if (track == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Track không tồn tại.");
            }

            // Tìm hoặc xác định RoundId
            string targetRoundId = request.Model.RoundId;
            if (string.IsNullOrEmpty(targetRoundId))
            {
                var firstRound = await _unitOfWork.GetRepository<Round>().GetQueryable()
                    .Where(r => r.EventId == track.EventId)
                    .OrderBy(r => r.RoundNumber)
                    .FirstOrDefaultAsync(cancellationToken);
                if (firstRound == null)
                {
                    return BaseException.BadRequestInvaildInputResponse("Sự kiện chưa có vòng thi nào.");
                }
                targetRoundId = firstRound.Id;
            }

            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(targetRoundId);
            if (round == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Vòng thi không tồn tại.");
            }

            if (track.EventId != team.EventId || round.EventId != team.EventId)
            {
                return BaseException.BadRequestInvaildInputResponse("Hạng mục hoặc Vòng thi này không thuộc sự kiện mà đội đang tham gia.");
            }

            var now = DateTime.UtcNow;
            var effectiveStartDate = track.StartDate ?? round.StartDate;
            var effectiveEndDate = track.EndDate ?? round.EndDate;

            if (now < effectiveStartDate)
            {
                return BaseException.BadRequestInvaildInputResponse("Hạng mục chưa mở, chưa thể nộp bài.");
            }
            if (now > effectiveEndDate)
            {
                return BaseException.BadRequestInvaildInputResponse("Đã hết hạn nộp bài cho hạng mục này.");
            }

            var roundPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                fr => fr.RoundId == round.Id, cancellationToken);
            if (roundPublished)
            {
                return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể nộp bài.");
            }

            // VÒNG SAU: đội phải ĐƯỢC ĐI TIẾP (IsAdvanced) từ vòng liền trước
            var prevRound = await _unitOfWork.GetRepository<Round>().GetQueryable()
                .AsNoTracking()
                .Where(r => r.EventId == round.EventId && r.RoundNumber < round.RoundNumber)
                .OrderByDescending(r => r.RoundNumber)
                .FirstOrDefaultAsync(cancellationToken);
            if (prevRound != null)
            {
                var advanced = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.TeamId == team.Id && fr.RoundId == prevRound.Id && fr.IsAdvanced,
                    cancellationToken);
                if (!advanced)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        $"Đội chưa vượt qua vòng trước ('{prevRound.RoundName}') nên không thể nộp bài cho vòng này.");
                }
            }

            // 3. Chống nộp trùng: mỗi đội chỉ nộp 1 bài cho mỗi cặp (Track, Round)
            var isAlreadySubmitted = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                sr => sr.TeamId == request.Model.TeamId && sr.TrackId == request.Model.TrackId && sr.RoundId == targetRoundId,
                cancellationToken);

            if (isAlreadySubmitted)
            {
                return BaseException.BadRequestDupplicationResponse("Nhóm đã nộp bài giải cho Hạng mục này ở Vòng thi này trước đó.");
            }

            var submitResult = new SubmitResult
            {
                TeamId = request.Model.TeamId,
                TrackId = request.Model.TrackId,
                RoundId = targetRoundId,
                SubmissionUrl = request.Model.SubmissionUrl,
                Description = request.Model.Description,
                IsActive = true,
                CreatedBy = currentUserId
            };

            await _unitOfWork.GetRepository<SubmitResult>().AddAsync(submitResult);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateSubmitResultResponseModel
            {
                Id = submitResult.Id,
                TeamId = submitResult.TeamId,
                TrackId = submitResult.TrackId,
                RoundId = submitResult.RoundId,
                SubmissionUrl = submitResult.SubmissionUrl,
                Description = submitResult.Description ?? string.Empty,
                IsActive = submitResult.IsActive,
                CreatedTime = submitResult.CreatedTime
            };
        }
    }
}
