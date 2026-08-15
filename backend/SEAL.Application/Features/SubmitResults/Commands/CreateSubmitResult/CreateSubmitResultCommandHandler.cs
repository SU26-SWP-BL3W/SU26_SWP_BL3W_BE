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
        private readonly SEAL_Application.Interfaces.IGitHostingService _gitHosting;

        public CreateSubmitResultCommandHandler(
            IUnitOfWork unitOfWork,
            SEAL_Application.Interfaces.ICurrentUserService currentUserService,
            SEAL_Application.Interfaces.IEventRoleChecker eventRoleChecker,
            SEAL_Application.Interfaces.IGitHostingService gitHosting)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _eventRoleChecker = eventRoleChecker;
            _gitHosting = gitHosting;
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

            if (team.Status == SEAL_Domain.Entity.Enums.TeamStatus.Disqualified)
            {
                return BaseException.BadRequestInvaildInputResponse("Đội đã bị loại nên không thể nộp bài.");
            }

            if (team.Status != SEAL_Domain.Entity.Enums.TeamStatus.Registered)
            {
                return BaseException.BadRequestInvaildInputResponse(
                    "Đội chưa đăng ký chính thức (chưa chốt đủ thành viên/được duyệt) nên chưa thể nộp bài.");
            }

            if (string.IsNullOrEmpty(team.TrackId) || request.Model.TrackId != team.TrackId)
            {
                return BaseException.BadRequestInvaildInputResponse("Đội chỉ được nộp bài cho hạng mục đã đăng ký.");
            }

            if (string.IsNullOrEmpty(request.Model.RoundId))
            {
                return BaseException.BadRequestInvaildInputResponse("RoundId không được để trống.");
            }

            var track = await _unitOfWork.GetRepository<Track>().GetByIdAsync(request.Model.TrackId);
            if (track == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Track không tồn tại.");
            }

            var round = await _unitOfWork.GetRepository<Round>().GetByIdAsync(request.Model.RoundId);
            if (round == null)
            {
                return BaseException.BadRequestInvaildInputResponse("Vòng thi không tồn tại.");
            }

            if (track.EventId != team.EventId || round.EventId != team.EventId)
            {
                return BaseException.BadRequestInvaildInputResponse("Hạng mục hoặc Vòng thi này không thuộc sự kiện mà đội đang tham gia.");
            }

            var now = DateTime.UtcNow;
            // Cửa sổ nộp theo Round — Track.EndDate là deadline cả event, không dùng để chặn từng vòng.
            if (now < round.StartDate)
            {
                return BaseException.BadRequestInvaildInputResponse("Vòng thi chưa mở, chưa thể nộp bài.");
            }
            if (now > round.EndDate)
            {
                return BaseException.BadRequestInvaildInputResponse("Đã hết hạn nộp bài cho vòng thi này.");
            }

            var roundPublished = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                fr => fr.RoundId == round.Id, cancellationToken);
            if (roundPublished)
            {
                return new BaseException.ForbiddenException("Kết quả vòng thi đã được tính/công bố nên không thể nộp bài.");
            }

            var prevRound = await _unitOfWork.GetRepository<Round>().GetQueryable()
                .AsNoTracking()
                .Where(r => r.EventId == round.EventId && r.RoundNumber < round.RoundNumber)
                .OrderByDescending(r => r.RoundNumber)
                .FirstOrDefaultAsync(cancellationToken);
            if (prevRound != null)
            {
                var advanced = await _unitOfWork.GetRepository<FinalResult>().AnyAsync(
                    fr => fr.TeamId == team.Id
                       && fr.RoundId == prevRound.Id
                       && fr.TrackId == team.TrackId
                       && fr.IsAdvanced,
                    cancellationToken);
                if (!advanced)
                {
                    return BaseException.BadRequestInvaildInputResponse(
                        $"Đội chưa vượt qua vòng trước ('{prevRound.RoundName}') ở hạng mục này nên không thể nộp bài.");
                }
            }

            var isAlreadySubmitted = await _unitOfWork.GetRepository<SubmitResult>().AnyAsync(
                sr => sr.TeamId == request.Model.TeamId && sr.TrackId == request.Model.TrackId && sr.RoundId == round.Id,
                cancellationToken);

            if (isAlreadySubmitted)
            {
                return BaseException.BadRequestDupplicationResponse("Nhóm đã nộp bài giải cho Hạng mục này ở Vòng thi này trước đó.");
            }

            var repoUrl = string.IsNullOrWhiteSpace(request.Model.RepoUrl) ? request.Model.SubmissionUrl : request.Model.RepoUrl;
            var inspect = await _gitHosting.InspectAsync(repoUrl, cancellationToken);
            if (inspect.ShouldReject)
            {
                return BaseException.BadRequestInvaildInputResponse(inspect.Error ?? "Repo không hợp lệ.");
            }

            var submitResult = new SubmitResult
            {
                TeamId = request.Model.TeamId,
                TrackId = request.Model.TrackId,
                RoundId = round.Id,
                RepoUrl = repoUrl,
                DemoUrl = request.Model.DemoUrl,
                SlideUrl = request.Model.SlideUrl,
                SubmissionUrl = repoUrl,
                RepoHost = inspect.Host,
                RepoFullName = inspect.FullName,
                RepoStars = inspect.Stars,
                RepoLastPush = inspect.LastPush,
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
                RepoUrl = submitResult.RepoUrl,
                DemoUrl = submitResult.DemoUrl,
                SlideUrl = submitResult.SlideUrl,
                RepoHost = submitResult.RepoHost,
                RepoFullName = submitResult.RepoFullName,
                RepoStars = submitResult.RepoStars,
                RepoLastPush = submitResult.RepoLastPush,
                Description = submitResult.Description ?? string.Empty,
                IsActive = submitResult.IsActive,
                CreatedTime = submitResult.CreatedTime
            };
        }
    }
}
